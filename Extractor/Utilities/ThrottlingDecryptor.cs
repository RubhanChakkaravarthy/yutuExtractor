using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jint;

namespace Extractor.Utilities
{
    public static class ThrottlingDecryptor
    {
        private static bool s_isInitialized;
        private static HttpClient s_client;
        private static Engine s_engine;
        private static Jint.Native.JsValue s_decryptFunction;
        private static Engine Engine
        {
            get
            {
                if (s_engine == null)
                    s_engine = new Engine();
                return s_engine;
            }
        }

        #region Patterns
        private static readonly Regex s_nParamPattern = new Regex("[&?]n=([^&]+)", RegexOptions.Compiled);
        private static readonly Regex s_decryptFunctionNamePattern = new Regex(
            "\\.get\\(\"n\"\\)\\)&&\\([a-zA-Z0-9$_]=([a-zA-Z0-9$_]+)(?:\\[(\\d+)])?\\([a-zA-Z0-9$_]\\)", RegexOptions.Compiled);
        private static readonly string s_decryptFunctionBodyPattern =
            "{0}=\\s*function([\\S\\s]*?\\}}\\s*return [\\w$]+?\\.join\\(\"\"\\)\\s*\\}};)";
        private static readonly string s_decrytFunctionNameArrayPattern = "{0}=\\[([a-zA-Z0-9$_, ]+)];";
        private static readonly Regex s_baseJsUrlPattern = new Regex("\\/s\\/player\\/[0-9a-zA-Z]+\\/player_ias.vflset\\/[a-zA-Z_]+\\/base.js");
        #endregion

        private static readonly Dictionary<string, string> s_decryptedNParamCache = new Dictionary<string, string>();
        
        /// <summary>
        /// Initialize throttling decryptor
        /// </summary>
        /// <param name="client">HttpClient</param>
        /// <exception cref="HttpRequestException"></exception>
        public static async Task InitAsync(HttpClient client)
        {
            s_isInitialized = true;
            s_client = client;

            if (s_decryptFunction == null) s_decryptFunction = await GetDecryptFunction();
        }

        /// <summary>
        /// Decrypt streaming url n value 
        /// </summary>
        /// <param name="streamingUrl">Streaming url to decrypt</param>
        /// <returns>Streaming url with decrypted n value</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string Apply(string streamingUrl)
        {
            if (!s_isInitialized)
                throw new InvalidOperationException("Throttling Decryptor is not initialized, Call ThrottlingDecryptor.InitAsync to initialize");
            

            if (!ContainsNParam(streamingUrl))
                return streamingUrl;

            try
            {
                string nParam = GetNParam(streamingUrl);
                if (!s_decryptedNParamCache.TryGetValue(nParam, out var decryptedNParam)) {
                    decryptedNParam = Engine.Invoke(s_decryptFunction, nParam).AsString();
                    s_decryptedNParamCache.Add(nParam, decryptedNParam);
                }

                return streamingUrl.Replace(nParam, decryptedNParam);
            }
            catch (Exception)
            {
                return streamingUrl;
            }
        }

        /// <summary>
        ///  Get base js url from youtube home page
        /// </summary>
        /// <returns>base js url</returns>
        /// <exception cref="HttpRequestException"></exception>
        private static async Task<string> GetBaseJsAsync()
        {
            var baseJsUrl = Constants.DefaultBaseJsUrl;
            using (var response = await s_client.GetAsync(Constants.YoutubeBaseUrl))
            {
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    baseJsUrl = Constants.YoutubeBaseUrl + s_baseJsUrlPattern.Match(content).Groups[0].Value;
                }
            }

            using (var response = await s_client.GetAsync(baseJsUrl))
            {
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();
                return null;
            }
        }

        private static async Task<Jint.Native.JsValue> GetDecryptFunction()
        {
            var baseJs = await GetBaseJsAsync();
            var decryptorFunctionStr = s_defaultDecryptorFunctionStr;
            var decryptFunctionName = Constants.DefaultThrottlingDecryptorFunctionName;
            if (baseJs != null)
            {
                var groups = ((IEnumerable<Group>)s_decryptFunctionNamePattern.Match(baseJs).Groups).ToList();
                var name = groups.ElementAtOrDefault(1)?.Value;
                var indexPresent = int.TryParse(groups.ElementAtOrDefault(2)?.Value, out int index);
                if (name != null)
                {
                    if (indexPresent)
                    {
                        var functionNameMatch = Regex.Match(baseJs, string.Format(s_decrytFunctionNameArrayPattern, name));
                        var array = functionNameMatch.Groups[1].Value;
                        decryptFunctionName = array.Split(',').Select(e => e.Trim()).ElementAt(index);
                    }

                    var functionBodyMatch = Regex.Match(baseJs, string.Format(s_decryptFunctionBodyPattern, decryptFunctionName));
                    decryptorFunctionStr = functionBodyMatch.Groups[0].Value;
                }
            }
            
            Engine.Evaluate(decryptorFunctionStr);
            return Engine.GetValue(decryptFunctionName);
        }

        private static bool ContainsNParam(string url)
        {
            return s_nParamPattern.IsMatch(url);
        }

        private static string GetNParam(string url)
        {
            return s_nParamPattern.Match(url).Groups[1].Value;
        }

        private static readonly string s_defaultDecryptorFunctionStr = @"var Qla = function(a) {
        var b = a.split(''), 
        c = [698968020, 1425986396, 1447042569, function() {
            for (var d = 64, e = []; ++d - e.length - 32; ) {
                switch (d) {
                case 58:
                    d -= 14;
                case 91:
                case 92:
                case 93:
                    continue;
                case 123:
                    d = 47;
                case 94:
                case 95:
                case 96:
                    continue;
                case 46:
                    d = 95
                }
                e.push(String.fromCharCode(d))
            }
            return e
        }
        , -1973572666, b, 342773615, function(d, e, f, h, l, m) {
            return e(h, l, m)
        }
        , 240007060, 1155442573, -193186023, -1967182956, 1573734553, null, 'const', function() {
            for (var d = 64, e = []; ++d - e.length - 32; )
                switch (d) {
                case 58:
                    d = 96;
                    continue;
                case 91:
                    d = 44;
                    break;
                case 65:
                    d = 47;
                    continue;
                case 46:
                    d = 153;
                case 123:
                    d -= 58;
                default:
                    e.push(String.fromCharCode(d))
                }
            return e
        }
        , 240007060, -1072684997, -1772436126, 1989329756, -278890574, function(d, e) {
            if (0 != e.length) {
                d = (d % e.length + e.length) % e.length;
                var f = e[0];
                e[0] = e[d];
                e[d] = f
            }
        }
        , 1434390514, 569250250, 522046826, -1369780405, -1215528139, function(d, e, f) {
            var h = d.length;
            e.forEach(function(l, m, n) {
                this.push(n[m] = d[(d.indexOf(l) - d.indexOf(this[m]) + m + h--) % d.length])
            }, f.split(''))
        }
        , -1237459209, 1672688733, -1069672953, -129790490, -2018512838, function(d) {
            for (var e = d.length; e; )
                d.push(d.splice(--e, 1)[0])
        }
        , null, function() {
            for (var d = 64, e = []; ++d - e.length - 32; )
                switch (d) {
                case 46:
                    d = 95;
                default:
                    e.push(String.fromCharCode(d));
                case 94:
                case 95:
                case 96:
                    break;
                case 123:
                    d -= 76;
                case 92:
                case 93:
                    continue;
                case 58:
                    d = 44;
                case 91:
                }
            return e
        }
        , -1887393104, function(d) {
            d.reverse()
        }
        , 445897340, 323324678, 2047015320, -1789574987, 1174868104, -1194797461, -1194797461, 1423833843, b, 1507253461, function() {
            for (var d = 64, e = []; ++d - e.length - 32; ) {
                switch (d) {
                case 91:
                    d = 44;
                    continue;
                case 123:
                    d = 65;
                    break;
                case 65:
                    d -= 18;
                    continue;
                case 58:
                    d = 96;
                    continue;
                case 46:
                    d = 95
                }
                e.push(String.fromCharCode(d))
            }
            return e
        }
        , 1840626491, 446690744, 351950909, 388191029, function(d, e) {
            0 != e.length && (d = (d % e.length + e.length) % e.length,
            e.splice(0, 1, e.splice(d, 1, e[0])[0]))
        }
        , /([/,-\x316,/])'/, 1529001094, -411793043, 1672688733, 615635612, b, 2024861189, function(d, e) {
            e = (e % d.length + d.length) % d.length;
            d.splice(-e).reverse().forEach(function(f) {
                d.unshift(f)
            })
        }
        , 1709754669, 1447042569, -1968928059, -500652165, 1358998087, 731691130, -2118591645, -2135526094, -1923307526, function(d, e) {
            e = (e % d.length + d.length) % d.length;
            d.splice(e, 1)
        }
        , 1830954891, -133096787, -1692869875, 1098247285, null, 815498581, -1991253791, 1915970014, 1351273735, 446690744, function(d, e, f, h, l) {
            return e(f, h, l)
        }
        , function(d, e) {
            for (d = (d % e.length + e.length) % e.length; d--; )
                e.unshift(e.pop())
        }
        ];
        c[13] = c;
        c[34] = c;
        c[76] = c;
        try {
            try {
                (7 > c[69] || ((0,
                c[82])((0,
                c[71])(c[34], c[32]), c[83], c[40], c[46]) % ((0,
                c[83])(c[51], c[5]),
                c[71])(c[46], c[55]),
                void 0)) && (0,
                c[Math.pow(7, 3) + 179 + -515])((0,
                c[71])(c[5], c[11]) === (0,
                c[21])(c[20], c[46]), c[53], (0,
                c[53])(c[22], c[46]), c[4], c[59]),
                4 >= c[41] && (-8 != c[36] && (((((0,
                c[7])((0,
                c[7])((0,
                c[71])(c[5], c[new Date('1970-01-01T02:00:25.000+02:00') / 1E3]), c[83], (0,
                c[83])(c[26], c[new Date('1970-01-01T07:01:16.000+07:00') / 1E3]), c[61], c[52]), c[54], (0,
                c[76])(c[53], c[27]), c[68], c[0]),
                (0,
                c[83])(c[27], c[69]),
                c[75])(c[74], c[56]),
                c[-177 * Math.pow(5, 3) - -22134])(c[Math.pow(4, 2) % 471 - -11], c[71]),
                c[59])(c[14]),
                (0,
                c[8])(c[38], c[56]),
                (0,
                c[63])((0,
                c[40])(c[80], c[2]), c[34], (0,
                c[46])(), c[2], c[47]),
                1) || (((((0,
                c[63])((0,
                c[63])((0,
                c[74])(c[15], c[44]), c[74], c[2], c[5]), c[34], (0,
                c[58])(), c[56], c[47]),
                (0,
                c[63])((0,
                c[63])((0,
                c[34])((0,
                c[13])(), c[56], c[47]), c[62], c[new Date('1970-01-01T02:30:32.000+02:30') / 1E3], c[487 - 118 * Math.pow(4, 1)]), c[74], c[new Date('1970-01-01T09:00:48.000+09:00') / 1E3], c[67]),
                (0,
                c[30 + Math.pow(8, 2) + -60])((0,
                c[13])(), c[2], c[47]) | (0,
                c[74])(c[2], c[73]),
                c[new Date('1970-01-01T11:31:14.000+11:30') / 1E3])(c[56], c[20]),
                c[74])(c[27], c[70]),
                c[138 - 320 % Math.pow(6, 3)])((0,
                c[58])(), c[56], c[47]),
                c[0])(c[2], c[52])),
                2 != c[75] && (-7 < c[65] && (((0,
                c[62])(c[65], c[2]),
                ((((((0,
                c[24])(c[56]),
                c[40])(c[72], c[69]),
                c[63])((0,
                c[63])((0,
                c[62])(c[43], c[27]), c[34], c[69], c[42]), c[47 + Math.pow(6, 3) + -261], c[72], c[-6 + 59 % Math.pow(2, 5)]),
                c[34])(c[60], c[63]),
                c[56])(c[13], c[50]),
                c[2])(c[75], c[63]),
                c[57])((0,
                c[2])(c[65], c[0]), c[Math.pow(5, 3) + 19456 + -19579], c[0], c[63]),
                []) || (0,
                c[57])(((((0,
                c[34])(c[45], c[50]),
                (0,
                c[28])((0,
                c[7])(), c[80], c[41]),
                (0,
                c[57])((0,
                c[57])((0,
                c[31])(c[17], c[16]), c[28], (0,
                c[40])(), c[9], c[41]), c[68], c[63], c[27]),
                c[Math.pow(2, 2) - 105 - -158])((0,
                c[28])((0,
                c[20])(), c[50], c[41]), c[68], c[50], c[76]),
                c[28])((0,
                c[7])(), c[50], c[41]),
                (0,
                c[288 + -45 * Math.pow(6, 1)])(c[42]),
                c[29 - Math.pow(7, 5) + 16846])(c[50], c[73]), c[31], c[50], c[58])),
                6 != c[new Date('1969-12-31T22:16:09.000-01:45') / 1E3] && (((((((0,
                c[22])(c[9]),
                c[18])(c[50]),
                c[0])(c[55], c[80]),
                c[22])(c[17]),
                c[81])(c[6], c[41]),
                (0,
                c[49])(c[64], c[41]),
                c[-80 * Math.pow(3, 1) + 255])(c[3], c[21]),
                c[15])(c[74], c[5]),
                -3 != c[70] && (3 == c[29] || ((0,
                c[26])((0,
                c[52])(c[Math.pow(3, 2) - 173 - -230], c[0]), c[Math.pow(4, 2) - 227 + 276], c[33], c[60]),
                0)) && (0,
                c[47])((0,
                c[76])((0,
                c[0])(), c[11], c[63]), c[70], c[50], c[3]),
                3 <= c[72] && (4 != c[26] ? (0,
                c[56])((0,
                c[20])(c[61], c[3]), c[12], (0,
                c[30])(c[25], c[13]), c[47]) : ((0,
                c[41])(c[53], c[new Date('1970-01-01T05:15:22.000+05:15') / 1E3]),
                c[30])((0,
                c[1])((0,
                c[64])(), c[23], c[14]), c[59], c[12], c[15]))
            } catch (d) {
                ((0,
                c[57])(c[82], c[66]),
                c[1])((0,
                c[25])(), c[53], c[14]),
                (0,
                c[21])((0,
                c[1])((0,
                c[64])(), c[23], c[14]), c[41], (0,
                c[1])((0,
                c[77])(), c[66], c[14]) === (0,
                c[59])(c[54], c[74]), c[23], c[67])
            }
        } catch (d) {
            return 'enhanced_except_-pgBhef-_w8_' + a
        }
        return b.join('')
    };";
    }
}

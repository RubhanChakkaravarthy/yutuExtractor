using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Extractor.ItemExtractors.Interface;
using Extractor.Models;
using Extractor.Models.Enums;
using Extractor.Models.Stream;
using Extractor.Utilities;

namespace Extractor.ItemExtractors
{
	
    public class StreamInfoExtractor : IItemExtractor<StreamInfo>
    {
        private readonly JsonObject _playerResponse;
        private readonly JsonObject _nextResponse;

        public StreamInfoExtractor(JsonObject playerResponse, JsonObject nextResponse)
        {
            _playerResponse = playerResponse;
            _nextResponse = nextResponse;
        }

        public string GetVideoId()
        {
            return _playerResponse.Get<string>("videoDetails.videoId");
        }

        public string GetName()
        {
            return _playerResponse.Get<string>("videoDetails.title");
        }

        public ThumbnailInfo[] GetThumbnails()
        {
            return ParsingHelpers.ParseThumbnails(_playerResponse.GetArray("videoDetails.thumbnail.thumbnails"));
        }

        public StreamType GetStreamType()
        {
            // TODO Get correct stream type
            return StreamType.VIDEO_STREAM;
        }

        public string GetUrl()
        {
            return ParsingHelpers.GetUrl(_nextResponse.GetObject("currentVideoEndpoint"));
        }

        public ItemType GetItemType()
        {
            return ItemType.STREAM;
        }

        public TimeSpan GetDuration()
        {
            return TimeSpan.FromSeconds(int.Parse(_playerResponse.Get<string>("videoDetails.lengthSeconds")));
        }

        public string GetDescription()
        {
            return _playerResponse.Get<string>("videoDetails.shortDescription");
        }

        public string GetPublishedTime()
        {
            var primaryVideoInfo = _nextResponse.GetArray("contents.twoColumnWatchNextResults.results.results.contents")
                .First(c => c.Has("videoPrimaryInfoRenderer"))
                .GetObject("videoPrimaryInfoRenderer");
            return ParsingHelpers.GetText(primaryVideoInfo.GetObject("dateText"));
        }

        public string GetViewCount()
        {
            return _playerResponse.Get<string>("videoDetails.viewCount");
        }

        public ChannelItem GetUploader()
        {

            var channelObj = _nextResponse.GetArray("contents.twoColumnWatchNextResults.results.results.contents")
                    .First(c => c.Has("videoSecondaryInfoRenderer"))
                    .GetObject("videoSecondaryInfoRenderer.owner.videoOwnerRenderer");
            var channel = new ChannelItem
            {
                ItemType = ItemType.CHANNEL,
                Name = ParsingHelpers.GetText(channelObj.GetObject("title")),
                ChannelId = ParsingHelpers.GetBrowseId(channelObj.GetObject("navigationEndpoint")),
                Url = ParsingHelpers.GetUrl(channelObj.GetObject("navigationEndpoint")),
                Thumbnails = ParsingHelpers.ParseThumbnails(channelObj.GetArray("thumbnail.thumbnails")),
                SubscribersCount = ParsingHelpers.GetText(channelObj.GetObject("subscriberCountText"))
            };

            if (channelObj.TryGetArray("badges", out var badges))
            {
	            channel.BadgeType = ParsingHelpers.ParseChannelBadgeType(badges);
            }

            return channel;
        }

        public List<VideoStream> GetVideoStreams()
        {
            var streams = _playerResponse.GetArray("streamingData.adaptiveFormats");
            var videoStreams = new List<VideoStream>();
            foreach (var stream in streams)
            {
                if (!ItagItem.ITAGS.TryGetValue(stream.Get<int>("itag"), out var itagItem) || !(itagItem.Type == ItagType.VIDEO || itagItem.Type == ItagType.VIDEO_ONLY))
	                continue;
                var videoStream = new VideoStream(itagItem)
                {
	                Width = stream.Get<int>("width"),
	                Height = stream.Get<int>("height"),
	                Quality = stream.Get<string>("quality"),
	                Fps = stream.Get<int>("fps")
                };
                AddBaseStreamData(videoStream, stream.AsObject());
                videoStreams.Add(videoStream);
            }

            return videoStreams;
        }

        public List<AudioStream> GetAudioStreams()
        {
            var streams = _playerResponse.GetArray("streamingData.adaptiveFormats");
            var audioStreams = new List<AudioStream>();
            foreach (var stream in streams)
            {
                var itagId = stream.Get<int>("itag");
                if (!ItagItem.ITAGS.TryGetValue(itagId, out var itagItem) || itagItem.Type != ItagType.AUDIO) 
	                continue;
                var audioStream = new AudioStream(itagItem)
                {
	                Channels = stream.Get<int>("audioChannels"),
	                SampleRate = int.Parse(stream.Get<string>("audioSampleRate")),
	                Quality = stream.Get<string>("audioQuality")
                };
                AddBaseStreamData(audioStream, stream.AsObject());
                audioStreams.Add(audioStream);
            }

            return audioStreams;
        }

        public CaptionItem[] GetCaptions()
        {
            return _playerResponse.GetArray("captions.playerCaptionsTracklistRenderer.captionTracks").Select(c => new CaptionItem
            {
                Name = c.Get<string>("name.simpleText"),
                Url = c.Get<string>("baseUrl"),
                LanguageCode = c.Get<string>("languageCode")
            }).ToArray();
        }


        private void AddBaseStreamData(BaseStream stream, JsonObject streamObj)
        {
            stream.InitStart = int.Parse(streamObj.Get<string>("initRange.start"));
            stream.InitEnd = int.Parse(streamObj.Get<string>("initRange.end"));
            stream.IndexStart = int.Parse(streamObj.Get<string>("indexRange.start"));
            stream.IndexEnd = int.Parse(streamObj.Get<string>("indexRange.end"));
            stream.Codec = ParseCodec(streamObj.Get<string>("mimeType"));
            stream.ApproxDurationMs = int.Parse(streamObj.Get<string>("approxDurationMs"));
            stream.ContentLength = int.Parse(streamObj.Get<string>("contentLength"));
            stream.AvgBitRate = streamObj.Get<int>("averageBitrate");
            stream.Url = DecryptStreamingUrl(streamObj.Get<string>("url"));
        }

        private string ParseCodec(string mimeType)
        {
            var codecStart = mimeType.IndexOf("\"", StringComparison.Ordinal) + 1;
            return mimeType.Substring(codecStart, mimeType.LastIndexOf("\"", StringComparison.Ordinal) - codecStart);
        }

        private string DecryptStreamingUrl(string url)
        {
            return ThrottlingDecryptor.Apply(url);
        }
    }
}
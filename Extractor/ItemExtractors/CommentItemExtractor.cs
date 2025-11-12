using System.Text.Json.Nodes;
using Extractor.ItemExtractors.Interface;
using Extractor.Models;
using Extractor.Models.Enums;
using Extractor.Utilities;

namespace Extractor.ItemExtractors
{
	internal class CommentItemExtractor : IItemExtractor<CommentItem>
	{
        private readonly JsonObject _commentItemObj;
        public CommentItemExtractor(JsonObject commentItemObj)
        {
            _commentItemObj = commentItemObj;
        }

		public ItemType GetItemType()
		{
			return ItemType.COMMENT;
        }

		public string GetName()
		{
            return ParsingHelpers
                .GetText(GetCommentRenderer().GetObject("authorText"));
		}

		public ThumbnailInfo[] GetThumbnails()
		{
			return ParsingHelpers
                .ParseThumbnails(GetCommentRenderer().GetArray("authorThumbnail.thumbnails"));
		}

		public string GetUrl()
		{
			return null;
		}

        public string GetContent()
        {
            return ParsingHelpers.GetText(GetCommentRenderer().GetObject("contentText"));
        }

        public string GetPublishedTime()
        {
            return ParsingHelpers.GetText(GetCommentRenderer().GetObject("publishedTimeText"));
        }

        public string GetCommentId()
        {
            return GetCommentRenderer().Get<string>("commentId");
        }

        public string GetAuthorUrl()
        {
            return ParsingHelpers.GetUrl(GetCommentRenderer().GetObject("authorEndpoint"));
        }

        public int GetLikeCount()
        {
            return int.Parse(ParsingHelpers.GetText(GetCommentRenderer().GetObject("voteCount")));
        }

        public int GetReplyCount()
        {
            return int.Parse(GetCommentRenderer().Get<string>("replyCount"));
        }

        public string GetRepliesContinuationToken()
        {
            return ParsingHelpers.ParseContinuationItemRenderer(_commentItemObj.GetArray("commentThreadRenderer.replies.commentRepliesRenderer.contents")
                .GetObject(0).GetObject("continuationItemRenderer"));
        }

        public bool IsPinned => GetCommentRenderer().Has("pinnedCommentBadge");

        public bool IsAuthorChannelOwner => GetCommentRenderer().Get<bool>("authorIsChannelOwner");

        private JsonObject GetCommentRenderer()
        {
            return _commentItemObj.GetObject("commentThreadRenderer.comment.commentRenderer");
        }
	}
}
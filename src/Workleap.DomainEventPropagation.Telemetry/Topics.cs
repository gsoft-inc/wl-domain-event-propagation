namespace Workleap.DomainEventPropagation
{
    public static class Topics
    {
        public static class Organization
        {
            internal const string Pattern = "-organization-egt-";
            public const string Name = "Organization";
            public const TopicType TopicType = Topics.TopicType.CustomTopic;
        }

        public static class Integrations
        {
            internal const string Pattern = "-integrations-egt-";
            public const string Name = "Integrations";
            public const TopicType TopicType = Topics.TopicType.CustomTopic;
        }

        public static class Signup
        {
            internal const string Pattern = "-signup-egt-";
            public const string Name = "Signup";
            public const TopicType TopicType = Topics.TopicType.CustomTopic;
        }

        public static IEnumerable<string> GetAllTopicsNames()
        {
            return GetAllTopics().Select(topicInfo => topicInfo.Name).ToList().AsReadOnly();
        }

        public static IEnumerable<string> GetAllTopicValidationPatterns()
        {
            return GetAllTopics().Select(topicInfo => topicInfo.Pattern).ToList().AsReadOnly();
        }

        public static string GetTopicValidationPattern(string topicName)
        {
            return GetAllTopics().First(topicInfo => topicInfo.Name == topicName).Pattern;
        }

        private static IEnumerable<TopicInfo> GetAllTopics()
        {
            return new List<TopicInfo>
            {
                new TopicInfo { Pattern = Organization.Pattern, Name = Organization.Name, TopicType = Organization.TopicType },
                new TopicInfo { Pattern = Integrations.Pattern, Name = Integrations.Name, TopicType = Integrations.TopicType },
                new TopicInfo { Pattern = Signup.Pattern, Name = Signup.Name, TopicType = Signup.TopicType },
            }.AsReadOnly();
        }

        private class TopicInfo
        {
            public string Pattern { get; set; }

            public string Name { get; set; }

            public TopicType TopicType { get; set; }
        }

        public enum TopicType
        {
            Undefined = 0,
            CustomTopic = 1,
            SystemTopic = 2
        }
    }
}

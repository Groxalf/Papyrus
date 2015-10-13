﻿using System;
using NSubstitute;
using NUnit.Framework;
using Papyrus.Business.Topics;
using Papyrus.Business.Topics.Exceptions;

namespace Papyrus.Tests.Business
{
    [TestFixture]
    public class TopicServiceShould
    {
        private TopicRepository topicRepo;
        private TopicService topicService;
        private VersionRange anyVersionRange;
        private ProductVersion2 anyProductVersion;
        private ProductVersion2 anotherProductVersion;
        private string anyProductId;

        [SetUp]
        public void SetUp()
        {
            topicRepo = Substitute.For<TopicRepository>();
            topicService = new TopicService(topicRepo);
            anyVersionRange = new VersionRange(fromVersionId: null, toVersionId: null);
            anyProductId = "AnyProductId";
        }

        [Test]
        [ExpectedException(typeof(CannotSaveTopicsWithNoRelatedProductException))]
        public async void fail_when_trying_to_save_topics_with_no_related_product()
        {
            var topic = new Topic(null);
            topic.AddVersionRange(anyVersionRange);

            await topicService.Create(topic);
        }

        [Test]
        [ExpectedException(typeof(CannotSaveTopicsWithNoVersionRangesException))]
        public async void fail_when_trying_to_save_topics_with_no_ranges()
        {
            var topic = new Topic(anyProductId);

            await topicService.Create(topic);
        }

        [Test]
        [ExpectedException(typeof(CannotSaveTopicsWithDefinedTopicIdException))]
        public async void fail_when_trying_to_save_topics_with_id()
        {
            var topic = new Topic(anyProductId)
                            .WithId("AnyTopicId");
            topic.AddVersionRange(anyVersionRange);

            await topicService.Create(topic);
        }

        [Test]
        public async void save_a_topic_when_it_is_created()
        {
            var topic = new Topic(anyProductId);
            topic.AddVersionRange(anyVersionRange);

            await topicService.Create(topic);

            topicRepo.Received().Save(topic);
            topicRepo.Received().Save(Arg.Is<Topic>(t => !String.IsNullOrEmpty(t.TopicId)));
        }

        [Test]
        [ExpectedException(typeof(CannotUpdateTopicsWithoutTopicIdDeclaredException))]
        public void fail_when_trying_to_update_a_topic_without_id()
        {
            var topic = new Topic(anyProductId);
            topic.AddVersionRange(anyVersionRange);

            topicService.Update(topic);
        }

        [Test]
        [ExpectedException(typeof(CannotUpdateTopicsWithNoVersionRangesException))]
        public void fail_when_trying_to_update_a_topic_with_no_ranges()
        {
            var topic = new Topic(anyProductId)
                .WithId("AnyTopicId");

            topicService.Update(topic);
        }

        [Test]
        public void update_a_topic_of_the_library()
        {
            var topic = new Topic(anyProductId)
                .WithId("AnyTopicId");
            topic.AddVersionRange(anyVersionRange);

            topicService.Update(topic);

            topicRepo.Received().Update(topic);
        }
    }
}
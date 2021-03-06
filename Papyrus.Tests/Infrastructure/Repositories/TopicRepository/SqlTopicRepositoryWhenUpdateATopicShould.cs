﻿using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Papyrus.Business.Documents;
using Papyrus.Business.Products;
using Papyrus.Business.Topics;
using Papyrus.Business.VersionRanges;
using Papyrus.Infrastructure.Core.Database;
using Papyrus.Tests.Infrastructure.Repositories.Helpers;

namespace Papyrus.Tests.Infrastructure.Repositories.TopicRepository
{
    [TestFixture]
    public class SqlTopicRepositoryWhenUpdateATopicShould : SqlTest
    {
        private SqlInserter sqlInserter;
        private SqlTopicCommandRepository sqlTopicRepository;
        private ProductVersion version1 = new ProductVersion("FirstPapyrusVersionId", "1.0", DateTime.Today.AddDays(-2));
        private ProductVersion version2 = new ProductVersion("SecondPapyrusVersionId", "2.0", DateTime.Today);

        [SetUp]
        public void Initialize()
        {
            sqlInserter = new SqlInserter(dbConnection);
            sqlTopicRepository = new SqlTopicCommandRepository(dbConnection);
        }

        [Test]
        public async Task removes_old_version_ranges_for_given_topic()
        {
            var topic = new Topic("PapyrusId").WithId("TopicId");
            var versionRange = new VersionRange(version1, version2)
                                                .WithId("VersionRangeId");
            topic.AddVersionRange(versionRange);
            await sqlInserter.Insert(topic);

            var topicToUpdate = new Topic("PapyrusId").WithId("TopicId");
            await sqlTopicRepository.Update(topicToUpdate);

            var oldVersionRanges = (await dbConnection.Query<dynamic>(@"SELECT FromVersionId, ToVersionId  
                                            FROM VersionRange 
                                            WHERE VersionRangeId = @VersionRangeId",
                                            new {VersionRangeId = "VersionRangeId"}));
            oldVersionRanges.Should().BeEmpty();
        }

        [Test]
        public async Task removes_old_documents_for_given_topic()
        {
            var topic = new Topic("PapyrusId").WithId("TopicId");
            var versionRange = new VersionRange(version1, version2)
                                                .WithId("VersionRangeId");
            var document = new Document("Título", "Descripción", "Contenido", "es-ES").WithId("DocumentId");
            versionRange.AddDocument(document);
            topic.AddVersionRange(versionRange);
            await sqlInserter.Insert(topic);

            var topicToUpdate = new Topic("PapyrusId").WithId("TopicId");
            await sqlTopicRepository.Update(topicToUpdate);

            var oldDocument = (await dbConnection.Query<Document>(@"SELECT Title, Description, Content, Language  
                                            FROM Document 
                                            WHERE DocumentId = @DocumentId",
                                            new { DocumentId = "DocumentId" })).FirstOrDefault();
            oldDocument.Should().BeNull();
        }

        [Test]
        public async Task save_its_new_version_ranges()
        {
            var topic = new Topic("PapyrusId").WithId("TopicId");
            await sqlInserter.Insert(topic);

            topic.AddVersionRange(new VersionRange(version1, version2).WithId("VersionRangeId"));
            await sqlTopicRepository.Update(topic);

            var newVersionRange = (await dbConnection.Query<dynamic>(@"SELECT FromVersionId, ToVersionId  
                                            FROM VersionRange 
                                            WHERE TopicId = @TopicId",
                                            new { TopicId = "TopicId" })).FirstOrDefault();
            Assert.That(newVersionRange.FromVersionId, Is.EqualTo(version1.VersionId));
            Assert.That(newVersionRange.ToVersionId, Is.EqualTo(version2.VersionId));
        }

        [Test]
        public async Task save_documents_for_each_of_its_version_ranges()
        {
            var topic = new Topic("PapyrusId").WithId("TopicId");
            await sqlInserter.Insert(topic);

            var versionRange = new VersionRange(version1, version2).WithId("VersionRangeId");
            versionRange.AddDocument(new Document("Título", "Descripción", "Contenido", "es-ES").WithId("DocumentId"));
            topic.AddVersionRange(versionRange);
            await sqlTopicRepository.Update(topic);

            var newVersionRange = (await dbConnection.Query<Document>(@"SELECT Title, Description, Content, Language  
                                                    FROM Document 
                                                    WHERE VersionRangeId = @VersionRangeId",
                                                    new { VersionRangeId = "VersionRangeId" })).FirstOrDefault();
            newVersionRange.Title.Should().Be("Título");
            newVersionRange.Description.Should().Be("Descripción");
            newVersionRange.Content.Should().Be("Contenido");
            newVersionRange.Language.Should().Be("es-ES");
        }
    }
}
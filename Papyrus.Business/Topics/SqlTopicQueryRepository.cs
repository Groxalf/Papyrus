﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Papyrus.Business.Exporters;
using Papyrus.Business.Products;
using Papyrus.Infrastructure.Core.Database;

namespace Papyrus.Business.Topics {
    public class SqlTopicQueryRepository : TopicQueryRepository {
        private readonly DatabaseConnection connection;

        public SqlTopicQueryRepository(DatabaseConnection connection) {
            this.connection = connection;
        }

        public async Task<List<TopicSummary>> GetAllTopicsSummaries() {
            var resultset = (await connection.Query<dynamic>(
                @"SELECT Topic.TopicId, Product.ProductName, Product.ProductId, VersionRange.ToVersionId, Document.Title, Document.Description
                    FROM Topic
                    JOIN Product ON Product.ProductId = Topic.ProductId
                    JOIN VersionRange ON VersionRange.TopicId = Topic.TopicId
                    JOIN Document ON Document.VersionRangeId = VersionRange.VersionRangeId"
                )).ToList();
            foreach (var topic in resultset) {
                if (topic.ToVersionId == "*") {
                    topic.VersionName = (await connection.Query<string>(
                        @"SELECT TOP 1 VersionName, Release FROM ProductVersion WHERE ProductId = @ProductId ORDER BY Release DESC"
                        , new {ProductId = topic.ProductId})).First();
                }
                else {
                    topic.VersionName = (await connection.Query<string>(
                        @"SELECT VersionName, Release FROM ProductVersion WHERE VersionId = @VersionId"
                        , new { VersionId = topic.ToVersionId })).First();
                }
            }
            var orderedResultSet = resultset.OrderBy(t => t.Release);
            var topicsToShow = DistinctByTopicChoosingTheRowWithLatestDocumentAdded(orderedResultSet);
            return topicsToShow;
        }

        public async Task<EditableTopic> GetEditableTopicById(string topicId) {
            var product = await GetRelatedProductFor(topicId);
            var observableVersionRanges = await GetVersionRangesOf(topicId);
            return new EditableTopic {
                TopicId = topicId,
                Product = product,
                VersionRanges = observableVersionRanges
            };
        }

        public async Task<List<ExportableTopic>> GetExportableTopicsForProductVersion(string productId, ProductVersion version) {
            var topics = await GetExportableTopicsForProduct(productId);

            foreach (var exportableTopic in topics) {
                var versionRanges = exportableTopic.VersionRanges;
                for (var index = 0; index < versionRanges.Count; index++) {
                    if (!versionRanges[index].Contains(version)) {
                        exportableTopic.VersionRanges.Remove(versionRanges[index]);
                    }
                }
            }
            return topics;
        }

        public async Task<List<ExportableTopic>> GetExportableTopicsForProduct(string productId) {
            var topicIds = await connection.Query<string>(@"SELECT TopicId FROM Topic WHERE ProductId = @ProductId;", 
                                                            new { ProductId = productId });
            return await GetAllExportableTopics(topicIds);
        }

        private async Task<List<ExportableTopic>> GetAllExportableTopics(IEnumerable<string> topicIds) {
            List<ExportableTopic> topics = new List<ExportableTopic>();
            foreach (var topicId in topicIds) {
                var topic = await ConstructExportableTopic(topicId);
                topics.Add(topic);
            }
            return topics;
        }

        private async Task<ExportableTopic> ConstructExportableTopic(string topicId) {
            var product = await SelectProductOf(topicId);
            var topic = new ExportableTopic(product);
            var versionRanges = await SelectVersionRangesFor(topicId);
            foreach (var versionRange in versionRanges) {
                var exportableVersionRange = await ConstructExportableVersionRange(versionRange);
                topic.AddVersionRange(exportableVersionRange);
            }
            return topic;
        }

        private async Task<ExportableProduct> SelectProductOf(string topicId) {
            var product = (await connection.Query<ExportableProduct>(@"SELECT Product.ProductId, Product.ProductName FROM Product
                                                                        JOIN Topic ON Topic.ProductId = Product.ProductId", 
                                                                        new {TopicId = topicId})).First();
            return product;
        }

        private async Task<IEnumerable<dynamic>> SelectVersionRangesFor(string topicId) {
            return await connection.Query<dynamic>(@"SELECT VersionRangeId, FromVersionId, ToVersionId 
                                                                        FROM VersionRange 
                                                                        WHERE TopicId = @TopicId;",
                new {TopicId = topicId});
        }

        private async Task<ExportableVersionRange> ConstructExportableVersionRange(dynamic versionRange) {
            var exportableVersionRange = new ExportableVersionRange();
            var productVersions = await SelectProductVersionsCorrespondingTo(versionRange);
            exportableVersionRange.AddVersions(productVersions);
            var documents = await SelectDocumentsCorrespondingTo(versionRange);
            exportableVersionRange.AddDocuments(documents);
            return exportableVersionRange;
        }

        private async Task<IEnumerable<ExportableDocument>> SelectDocumentsCorrespondingTo(dynamic versionRange) {
            return await connection.Query<ExportableDocument>(@"SELECT Title, Content, Language
                                                                        FROM Document
                                                                        WHERE VersionRangeId = @VersionRangeId",
                new {VersionRangeId = versionRange.VersionRangeId});
        }

        private async Task<IEnumerable<ProductVersion>> SelectProductVersionsCorrespondingTo(dynamic versionRange) {
            var fromProductVersion = (await connection.Query<dynamic>(@"SELECT ProductId, VersionName, Release
                                                                                FROM ProductVersion
                                                                                WHERE VersionId = @FromVersionId",
                                                                                new { FromVersionId = versionRange.FromVersionId })).First();
            var toProductVersion = (await connection.Query<dynamic>(@"SELECT ProductId, VersionName, Release
                                                                                FROM ProductVersion
                                                                                WHERE VersionId = @ToVersionId",
                                                                                new { ToVersionId = versionRange.ToVersionId })).First();
            return await connection.Query<ProductVersion>(@"SELECT VersionId, VersionName, Release
                                                                            FROM ProductVersion
                                                                            WHERE @FromVersion <= Release AND Release <= @ToVersion
                                                                                    AND ProductId = @ProductId",
                new {
                    FromVersion = fromProductVersion.Release,
                    ToVersion = toProductVersion.Release,
                    ProductId = fromProductVersion.ProductId
                });
        }

        private static List<TopicSummary> DistinctByTopicChoosingTheRowWithLatestDocumentAdded(IEnumerable<dynamic> dynamicTopics) {
            return dynamicTopics.GroupBy(topic => topic.TopicId)
                .Select(topics => topics.First())
                .Select(TopicSummaryFromDynamic)
                .ToList();
        }

        private static TopicSummary TopicSummaryFromDynamic(dynamic topic) {
            return new TopicSummary {
                TopicId = topic.TopicId,
                Product = new DisplayableProduct { ProductId = topic.ProductId, ProductName = topic.ProductName },
                VersionName = topic.VersionName,
                LastDocumentTitle = topic.Title,
                LastDocumentDescription = topic.Description,
            };
        }

        private async Task<DisplayableProduct> GetRelatedProductFor(string topicId) {
            var product = (await connection.Query<DisplayableProduct>(@"SELECT Product.ProductId, Product.ProductName FROM Topic
                                                                        JOIN Product ON Topic.TopicId = @TopicId AND
                                                                                        Topic.ProductId = Product.ProductId",
                new { TopicId = topicId }))
                .FirstOrDefault();
            return product;
        }

        private async Task<ObservableCollection<EditableVersionRange>> GetVersionRangesOf(string topicId) {
            var versionRanges = (await connection
                .Query<dynamic>(@"SELECT VersionRangeId, FromVersionId, ToVersionId 
                                FROM VersionRange WHERE TopicId = @TopicId",
                                new { TopicId = topicId })).ToList();
            foreach (var versionRange in versionRanges) {
                await AssignProductVersionTo(versionRange);
            }
            await AddDocumentsForEachVersionRangeIn(versionRanges);
            var editableVersionRanges = versionRanges.Select(MapToEditableVersionRange);
            return new ObservableCollection<EditableVersionRange>(editableVersionRanges);
        }

        private async Task AssignProductVersionTo(dynamic versionRange) {
            string fromVersionId = versionRange.FromVersionId;
            versionRange.FromVersion = await SelectProductVersionById(fromVersionId);
            if (versionRange.ToVersionId == LastProductVersion.Id) {
                versionRange.ToVersion = new LastProductVersion();
                return;
            }
            var toVersionId = versionRange.ToVersionId;
            versionRange.ToVersion = SelectProductVersionById(toVersionId);
        }

        private async Task<ProductVersion> SelectProductVersionById(string id) {
            return (await connection
                .Query<ProductVersion>(@"SELECT VersionId, VersionName, Release FROM ProductVersion WHERE VersionId = @VersionId",
                    new {VersionId = id})).First();
        }

        private async Task AddDocumentsForEachVersionRangeIn(IEnumerable<dynamic> versionRanges) {
            foreach (var versionRange in versionRanges) {
                versionRange.Documents = await GetDocumentsOf(versionRange);
            }
        }

        private EditableVersionRange MapToEditableVersionRange(dynamic versionRange) {
            var fromVersion = versionRange.FromVersion;
            var toVersion = versionRange.ToVersion;
            var editableVersionRange = new EditableVersionRange() {
                FromVersion = versionRange.FromVersion,
                ToVersion = versionRange.ToVersion
            };

            foreach (var editableDocument in versionRange.Documents) {
                editableVersionRange.Documents.Add(editableDocument);
            }
            return editableVersionRange;
        }

        private async Task<List<EditableDocument>> GetDocumentsOf(dynamic versionRange) {
            var documents = (await connection.Query<EditableDocument>(@"SELECT Title, Description, Content, Language
                                                                            FROM Document
                                                                            WHERE VersionRangeId = @VersionRangeId",
                new { VersionRangeId = versionRange.VersionRangeId }))
                .ToList();
            return documents;
        }
    }
}
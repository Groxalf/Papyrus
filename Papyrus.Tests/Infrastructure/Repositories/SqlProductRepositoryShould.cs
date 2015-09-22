﻿using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Papyrus.Business.Products;
using Papyrus.Infrastructure.Core.Database;

namespace Papyrus.Tests.Infrastructure.Repositories
{
    [TestFixture]
    public class SqlProductRepositoryShould : SqlTest
    {
        [SetUp]
        public async void TruncateDataBase()
        {
            await dbConnection.Execute("TRUNCATE TABLE ProductVersion");
        }

        [Test]
        public async void load_a_product()
        {
            await InsertProductWith(
                productId: "AnyProductID", versionId:"1", productName: "AnyProductName", versionName:"AnyVersionName", description: "AnyDescription"
            );

            var product = await new SqlProductRepository(dbConnection).GetProduct("AnyProductId");

            product.Id.Should().Be("AnyProductID");
            product.Versions.First().VersionId.Should().Be("1");
            product.Versions.Count.Should().Be(1);

            product.Description.Should().Be("AnyDescription");
        }

        [Test]
        public async void return_null_when_try_to_load_an_no_existing_product()
        {
            var product = await new SqlProductRepository(dbConnection).GetProduct("AnyId");

            product.Should().Be(null);
        }

        [Test]
        public async Task load_a_list_with_all_products()
        {
            await InsertProductWith(productId: "1", versionId: "1", productName: "anyProductName", versionName: "anyVersionName");
            await InsertProductWith(productId: "2", versionId: "1", productName: "anyProductName", versionName: "anyVersionName");

            var products = await new SqlProductRepository(dbConnection).GetAllProducts();

            products.Should().Contain(prod => prod.Id == "1");
            products.Should().Contain(prod => prod.Id == "2");
            products.ToArray().Length.Should().Be(2);
        }


        private async Task InsertProductWith(string productId, string versionId, string productName, string versionName, string description = null)
        {
            await dbConnection.Execute(@"INSERT ProductVersion(ProductId, VersionId, ProductName, VersionName, Description) 
                                VALUES (@ProductId, @VersionId, @ProductName, @VersionName, @Description);",
                                new {
                                    ProductId = productId,
                                    VersionId = versionId,
                                    ProductName = productName,
                                    VersionName = versionName,
                                    Description = description
                                });
        }
    }
}
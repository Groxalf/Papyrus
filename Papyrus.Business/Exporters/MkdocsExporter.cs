﻿using System.IO;
using System.Threading.Tasks;
using Papyrus.Business.Products;

namespace Papyrus.Business.Exporters {
    public class MkdocsExporter {
        private const string YmlFileName = "mkdocs.yml";
        private const string MarkDownExtension = ".md";

        public async Task Export(WebSite webSite, string path) {
            var docsPath = Path.Combine(path, "docs");
            var docsDirectory = Directory.CreateDirectory(docsPath);
            foreach (var document in webSite.documents) {
                await ExportDocumentIn(document, docsDirectory);
            }
            await WriteYmlFileIn(path);
        }

        private static async Task WriteYmlFileIn(string path) {
            var ymlPath = Path.Combine(path, YmlFileName);
            await FileWriter.WriteFileWithContent(ymlPath, "");
        }

        private static async Task ExportDocumentIn(ExportableDocument document, DirectoryInfo directory) {
            var documentDirectory = directory.CreateSubdirectory(document.Route);
            var documentPath = Path.Combine(documentDirectory.FullName, document.Title + MarkDownExtension);
            await FileWriter.WriteFileWithContent(documentPath, document.Content);
        }
    }
}
namespace Papyrus.Tests
{
    public interface DocumentRepository
    {
        void Save(Document document);
        Document GetDocument(string id);
    }
}
using QuestPDF.Infrastructure;

namespace VCheckViewer_Others.Lib.Interface
{
    public interface IDocument
    {
        DocumentMetadata GetMetdata();
        DocumentSettings GetSettings();
        void Compose(IDocumentContainer container);
    }
}

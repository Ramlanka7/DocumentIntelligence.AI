using AI.DocumentIntelligence.Infrastructure.Documents.Processors;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using FluentAssertions;
using P = DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;

namespace AI.DocumentIntelligence.Tests.Documents;

public sealed class PowerPointDocumentProcessorTests
{
    private readonly PowerPointDocumentProcessor _sut = new();

    [Theory]
    [InlineData("slides.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    [InlineData("SLIDES.PPTX", "application/octet-stream")]
    public void CanProcess_ReturnsTrueForPptxExtensionOrContentType(string fileName, string contentType)
    {
        var byExtension = fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase);
        var byMime = contentType == "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        _sut.CanProcess(fileName, contentType).Should().Be(byExtension || byMime);
    }

    [Fact]
    public void CanProcess_ReturnsFalseForDocx()
    {
        _sut.CanProcess("file.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithTwoSlides_ReturnsTwoPages()
    {
        using var stream = CreatePptxWithSlides(["Slide One Content", "Slide Two Content"]);

        var result = await _sut.ProcessAsync(stream, "deck.pptx",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation");

        result.IsSuccess.Should().BeTrue();
        result.Value.Pages.Should().HaveCount(2);
        result.Value.Metadata.PageCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessAsync_WithSlides_FullTextContainsSlideContent()
    {
        using var stream = CreatePptxWithSlides(["Hello from slide one"]);

        var result = await _sut.ProcessAsync(stream, "deck.pptx",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation");

        result.IsSuccess.Should().BeTrue();
        result.Value.FullText.Should().Contain("Hello from slide one");
    }

    [Fact]
    public async Task ProcessAsync_PopulatesMetadata()
    {
        using var stream = CreatePptxWithSlides(["Content"]);

        var result = await _sut.ProcessAsync(stream, "presentation.pptx",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation");

        result.IsSuccess.Should().BeTrue();
        result.Value.Metadata.FileName.Should().Be("presentation.pptx");
        result.Value.Metadata.ContentType.Should().Be(
            "application/vnd.openxmlformats-officedocument.presentationml.presentation");
    }

    private static MemoryStream CreatePptxWithSlides(IEnumerable<string> slideTexts)
    {
        var ms = new MemoryStream();
        using var pptDoc = PresentationDocument.Create(ms, PresentationDocumentType.Presentation, true);

        var presentationPart = pptDoc.AddPresentationPart();
        presentationPart.Presentation = new P.Presentation(
            new P.SlideIdList(),
            new P.SlideSize { Cx = 9144000, Cy = 6858000 },
            new P.NotesSize { Cx = 6858000, Cy = 9144000 });

        var slideIdList = presentationPart.Presentation.SlideIdList!;
        uint slideId = 256;

        foreach (var text in slideTexts)
        {
            var slidePart = presentationPart.AddNewPart<SlidePart>();
            var slide = CreateSlide(text);
            slidePart.Slide = slide;
            slide.Save(slidePart);

            slideIdList.Append(new P.SlideId
            {
                Id = slideId++,
                RelationshipId = presentationPart.GetIdOfPart(slidePart),
            });
        }

        presentationPart.Presentation.Save();
        ms.Position = 0;
        return ms;
    }

    private static P.Slide CreateSlide(string text)
    {
        return new P.Slide(
            new P.CommonSlideData(
                new P.ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new P.ApplicationNonVisualDrawingProperties()),
                    new P.GroupShapeProperties(new D.TransformGroup()),
                    new P.Shape(
                        new P.NonVisualShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 2U, Name = "Content" },
                            new P.NonVisualShapeDrawingProperties(),
                            new P.ApplicationNonVisualDrawingProperties()),
                        new P.ShapeProperties(),
                        new P.TextBody(
                            new D.BodyProperties(),
                            new D.Paragraph(new D.Run(new D.Text(text))))))),
            new P.ColorMapOverride(new D.MasterColorMapping()));
    }
}

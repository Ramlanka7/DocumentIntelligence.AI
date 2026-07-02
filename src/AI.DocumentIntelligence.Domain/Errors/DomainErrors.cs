using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Domain.Errors;

/// <summary>
/// Central catalog of expected domain failures. Handlers return these via <see cref="Result"/>
/// instead of throwing, keeping control flow exception-free.
/// </summary>
public static class DomainErrors
{
    public static class User
    {
        public static readonly Error NotFound =
            Error.NotFound("User.NotFound", "The requested user was not found.");

        public static readonly Error EmailAlreadyInUse =
            Error.Conflict("User.EmailAlreadyInUse", "A user with this email already exists.");

        public static readonly Error InvalidEmail =
            Error.Validation("User.InvalidEmail", "The email address is not valid.");

        public static readonly Error InvalidCredentials =
            Error.Unauthorized("User.InvalidCredentials", "The supplied credentials are invalid.");

        public static readonly Error Inactive =
            Error.Forbidden("User.Inactive", "The user account is inactive.");
    }

    public static class Document
    {
        public static readonly Error NotFound =
            Error.NotFound("Document.NotFound", "The requested document was not found.");

        public static readonly Error MissingFileName =
            Error.Validation("Document.MissingFileName", "A file name is required.");

        public static readonly Error InvalidFileSize =
            Error.Validation("Document.InvalidFileSize", "File size must be greater than zero.");

        public static readonly Error InvalidPageCount =
            Error.Validation("Document.InvalidPageCount", "Page count cannot be negative.");

        public static readonly Error UnsupportedType =
            Error.Validation("Document.UnsupportedType", "The document format is not supported.");

        public static readonly Error NotProcessed =
            Error.Conflict("Document.NotProcessed", "The document has not finished processing.");
    }

    public static class Citation
    {
        public static readonly Error InvalidDocumentId =
            Error.Validation("Citation.InvalidDocumentId", "A citation must reference a valid document identifier.");

        public static readonly Error MissingDocumentName =
            Error.Validation("Citation.MissingDocumentName", "A citation must reference a document name.");

        public static readonly Error InvalidPageNumber =
            Error.Validation("Citation.InvalidPageNumber", "A citation page number must be 1 or greater.");

        public static readonly Error InvalidConfidenceScore =
            Error.Validation("Citation.InvalidConfidenceScore", "Confidence score must be between 0 and 1.");

        public static readonly Error Required =
            Error.Validation("Citation.Required", "AI responses must include at least one citation.");
    }

    public static class Analysis
    {
        public static readonly Error NotFound =
            Error.NotFound("Analysis.NotFound", "The requested analysis session was not found.");

        public static readonly Error NoDocuments =
            Error.Validation("Analysis.NoDocuments", "An analysis requires at least one document.");

        public static readonly Error TooManyDocuments =
            Error.Validation("Analysis.TooManyDocuments", "An analysis supports at most 4 documents.");
    }

    public static class Comparison
    {
        public static readonly Error NotFound =
            Error.NotFound("Comparison.NotFound", "The requested comparison session was not found.");

        public static readonly Error InsufficientDocuments =
            Error.Validation("Comparison.InsufficientDocuments", "A comparison requires between 2 and 4 documents.");
    }

    public static class Chat
    {
        public static readonly Error SessionNotFound =
            Error.NotFound("Chat.SessionNotFound", "The requested chat session was not found.");

        public static readonly Error EmptyMessage =
            Error.Validation("Chat.EmptyMessage", "A chat message cannot be empty.");

        public static readonly Error NoDocuments =
            Error.Validation("Chat.NoDocuments", "A chat session requires at least one document.");
    }

    public static class Session
    {
        public static readonly Error InvalidStateTransition =
            Error.Conflict("Session.InvalidStateTransition", "The session cannot transition to the requested state.");
    }

    public static class Upload
    {
        public static readonly Error TooManyDocuments =
            Error.Validation("Upload.TooManyDocuments", "A maximum of 4 documents may be uploaded at once.");

        public static readonly Error CombinedPageLimitExceeded =
            Error.Validation("Upload.CombinedPageLimitExceeded", "The combined page count of all uploaded documents must not exceed 500.");

        public static readonly Error FileSizeExceeded =
            Error.Validation("Upload.FileSizeExceeded", "One or more files exceed the maximum allowed size.");

        public static readonly Error CombinedSizeExceeded =
            Error.Validation("Upload.CombinedSizeExceeded", "The combined size of all uploaded files exceeds the allowed limit.");

        public static readonly Error UnsupportedFileType =
            Error.Validation("Upload.UnsupportedFileType", "One or more files have an unsupported or unrecognised file type.");
    }

    public static class Token
    {
        public static readonly Error Invalid =
            Error.Unauthorized("Token.Invalid", "The token is invalid or expired.");
    }

    public static class Export
    {
        public static readonly Error UnsupportedFormat =
            Error.Validation("Export.UnsupportedFormat", "The requested export format is not supported.");

        public static readonly Error EmptyResult =
            Error.Validation("Export.EmptyResult", "Cannot export a null result.");

        public static Error ExportFailed(string detail) =>
            Error.Failure("Export.Failed", $"Export generation failed: {detail}");
    }
}

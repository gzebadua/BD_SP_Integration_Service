using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BestDoctors.Integration.SharePoint
{
    public class Constants
    {
        public class DocumentListLibraries
        {
            public const string cApprovalStatus = "_ModerationStatus";
            public const string cCaseNoteFolder = "Name";
            public const string cCaseNoteFolderTitle = "Title";
            public const string cCaseNotes = "CaseNotes";
            public const string cCaseNotesSubject = "Subject";
            public const string cClosedCase = "ClosedCaseSummaries";
            public const string cDocAuthor = "Editor";
            public const string cDocIcon = "DocIcon";
            public const string cDocName = "LinkFilenameNoMenu";
            public const string cDocNameWithPopOutMenu = "LinkFilename";
            public const string cDocumentType = "Category";
            public const string cEditButton = "Edit";
            public const string cExternalDocuments = "ExternalDocuments";

            public const string cGroupQuery =
                "<GroupBy Collapse='TRUE' GroupLimit='100'><FieldRef Name='{0}'/></GroupBy><OrderBy><FieldRef Name='{1}' Ascending='FALSE'/></OrderBy>";

            public const string cInternalDocuments = "InternalDocuments";
            public const char cLobSeperator = '_';
            public const string cModifiedDate = "Modified";
            public const string cViewName = "BestDoctors";
            public const string cViewNameDatSheet = "DataSheet";
        }

        /// <summary>
        /// String[] arrays containing document types per lob and per list
        /// Make sure the variable name is ALWAYS [LineOfBusiness_DocumentListLibraries]
        /// e.g USGH_InternalDocuments.
        /// This allows for sustainability in adding more LOBs down the line
        /// </summary>
        public class DocumentTypes
        {
            public static string[] CAGH_CaseNotes =
                {
                    "Associate Director Notes", "Adjustor Notes",
                    "Administrative Notes", "Conference Call Notes", "Nurse Case Manager Notes", "Physician Notes",
                    "Initial Referral", "Case Closure"
                };

            public static string[] CAGH_ClosedCaseSummaries =
                {
                    "MDC Clinical Summary", "Expert Summary",
                    "BD Outcome Summary"
                };

            public static string[] CAGH_ExternalDocuments =
                {
                    "Clinical Summary", "Expert Reports", "Final Reports",
                    "General Record", "Labs", "Member Medical Records", "Pathology", "Radiology/ Cardiac",
                    "Expert Biography"
                };

            public static string[] CAGH_InternalDocuments =
                {
                    "Member Release Forms", "Survey/ Evaluation", "Invoices",
                    "Referral Form", "Impact Assessment"
                };

            public static string[] EU_CaseNotes =
                {
                    "Associate Director Notes", "Adjustor Notes", "Administrative Notes",
                    "Conference Call Notes", "Nurse Case Manager Notes", "Physician Notes", "Initial Referral",
                    "Case Closure"
                };

            public static string[] EU_ClosedCaseSummaries =
                {
                    "MDC Clinical Summary", "Expert Summary",
                    "BD Outcome Summary"
                };

            public static string[] EU_ExternalDocuments =
                {
                    "Clinical Summary", "Expert Reports", "Final Reports",
                    "General Record", "Labs", "Member Medical Records", "Pathology", "Radiology/ Cardiac",
                    "Expert Biography"
                };

            public static string[] EU_InternalDocuments =
                {
                    "Member Release Forms", "Survey/ Evaluation", "Invoices",
                    "Referral Form", "Impact Assessment"
                };

            public static string[] USGH_ExternalDocuments =
                {
                    "Clinical Summary", "Expert Reports", "Final Reports",
                    "General Record", "Labs", "Member Medical Records", "Pathology", "Radiology/ Cardiac",
                    "Expert Biography"
                };

            public static string[] USGH_InternalDocuments =
                {
                    "Member Release Forms", "Survey/ Evaluation", "Invoices",
                    "Referral Form", "Impact Assessment"
                };

            public static string[] WC_CaseNotes =
                {
                    "Associate Director Notes", "Adjustor Notes", "Administrative Notes",
                    "Conference Call Notes", "Nurse Case Manager Notes", "Physician Notes", "Initial Referral",
                    "Case Closure"
                };

            public static string[] WC_ClosedCaseSummaries =
                {
                    "MDC Clinical Summary", "Expert Summary",
                    "BD Outcome Summary"
                };

            public static string[] WC_ExternalDocuments =
                {
                    "Clinical Summary", "Expert Reports", "Final Reports",
                    "General Record", "Labs", "Member Medical Records", "Pathology", "Radiology/ Cardiac",
                    "Expert Biography"
                };

            public static string[] WC_InternalDocuments =
                {
                    "Member Release Forms", "Survey/ Evaluation", "Invoices",
                    "Referral Form", "Impact Assessment"
                };
        }

        public class LineOfBusiness
        {
            public const string cCAGH = "CAGH";
            public const string cEU = "EU";
            public const string cUSGH = "USGH";
            public const string cWC = "WC";
            public static string[] cLineOfBusiness = { cUSGH, cCAGH, cEU, cWC };
        }

        public class Misc
        {
            public const string cCaseLanding = "Case";
            public const string cLoggingCategoryName = "BestDoctors.DocRepository";
        }

        public class Permissions
        {
            public const string cAllAccess = "AllAccess";
            public const string cApprovers = "Approvers";
            public const string closedCaseAccess = "ClosedCaseAccess";
            public const string cPortalGuardTokenPrefix = "sso|";
            public const string cStandardWindowsClaim = "i:0#.w|";
            public const string cUniqueClaimType = "i:05.t|";
        }
    }
}
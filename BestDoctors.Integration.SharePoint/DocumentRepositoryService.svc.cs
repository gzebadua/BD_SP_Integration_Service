using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using BestDoctors.Integration.Schemas.Shared.Requests;
using FluentValidation.Results;
using Microsoft.SharePoint;

namespace BestDoctors.Integration.SharePoint
{

    /// <summary>
    /// This class contains the web service that runs on a SharePoint server and
    /// handles the management of all cases and all dynamic document library groups.
    /// </summary>
    public class DocumentRepositoryService : IDocumentRepository, IDisposable
    {
        /// <summary>
        /// This is the URL for the Document Repository site.
        /// </summary>
        private string _siteUrl;

        /// <summary>
        /// This is the current request received by the web service.
        /// </summary>
        private DocumentRepositoryRequest Request;

        /// <summary>
        /// This is the results returned by the web service.
        /// </summary>
        private DocumentRepositoryRequest Results;

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// This method is the only operation for the web service. It runs the actions with ElevatedPrivileges as required by SharePoint
        /// <paramref name="request"/> contains the action that needs to be performed.
        /// </summary>
        /// <param name="request">This is the request.</param>
        /// <returns>
        /// This returns information about the success or failure of the web service.
        /// </returns>
        public DocumentRepositoryRequest Submit(DocumentRepositoryRequest request)
        {
            Request = request;
            Results = request;
            Results.ValidationResults = new ValidationResult();
            try
            {
                _siteUrl = ConfigurationManager.ConnectionStrings["TargetSharePointServer"].ConnectionString.ToString();
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    PerformActions(request);
                });
            }
            catch (Exception GenEx)
            {
                //Results.ValidationResults.Errors.Add(new ValidationFailure("Submit", GenEx.Message));
                AddException("Submit", _siteUrl, GenEx);
            }
            return Results;
        }

        /// <summary>
        /// Performs different actions according to the incoming request
        /// </summary>
        /// <param name="request"></param>
        protected void PerformActions(DocumentRepositoryRequest request)
        {
            var lineOfBusiness = "";
            try
            {
                lineOfBusiness = request.CaseName.Split('-')[0].ToUpper();
            }
            catch (Exception)
            {
                this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                    "PerformActions",
                    String.Format("Error at PerformActions on '{0}' with case value {1} : Case Name Format is invalid", this.GetType().Name, request.CaseName)));
                return;
            }
            var thisIsUsghLob = lineOfBusiness.Contains(Constants.LineOfBusiness.cUSGH);
            try
            {
                using (var site = new SPSite(_siteUrl))
                {
                    var web = site.RootWeb;
                    web.AllowUnsafeUpdates = true;
                    PerformCaseActions(
                        web,
                        request,
                        string.Format("{0}{1}{2}", lineOfBusiness, Constants.DocumentListLibraries.cLobSeperator, Constants.DocumentListLibraries.cInternalDocuments));
                    PerformCaseActions(
                        web,
                        request,
                        string.Format("{0}{1}{2}", lineOfBusiness, Constants.DocumentListLibraries.cLobSeperator, Constants.DocumentListLibraries.cExternalDocuments));
                    if (!thisIsUsghLob)
                    {
                        PerformCaseActions(
                            web,
                            request,
                            string.Format("{0}{1}{2}", lineOfBusiness, Constants.DocumentListLibraries.cLobSeperator, Constants.DocumentListLibraries.cClosedCase));
                        PerformCaseActions(
                            web,
                            request,
                            string.Format("{0}{1}{2}", lineOfBusiness, Constants.DocumentListLibraries.cLobSeperator, Constants.DocumentListLibraries.cCaseNotes));
                    }
                    web.CustomUploadPage = "";
                    web.AllowUnsafeUpdates = false;
                }
            }
            catch (Exception GenEx)
            {
                AddException("PerformActions.SPSite", _siteUrl, GenEx);
            }
        }

        /// <summary>
        /// Performs actions (open, add, remove, close) on a per-case/per-library basis
        /// </summary>
        /// <param name="web"></param>
        /// <param name="request"></param>
        /// <param name="libraryName"></param>
        protected void PerformCaseActions(SPWeb web, DocumentRepositoryRequest request, string libraryName)
        {
            var caseFolder = AddOrFindCase(web, libraryName, request.CaseName);
            if (caseFolder == null)
            {
                this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                    "PerformCaseActions",
                    String.Format("Error at PerformCaseActions on '{0}' with case value {1} : No such case folder", this.GetType().Name, request.CaseName)));
                return;
            }
            caseFolder.Item.BreakRoleInheritance(true);

            switch (request.Action)
            {
                case DocumentRepositoryRequest.StorageAction.OpenCase:
                    AddCaseUser(web, caseFolder, request.CaseUsers);
                    break;

                case DocumentRepositoryRequest.StorageAction.AddCaseUser:
                    AddCaseUser(web, caseFolder, request.CaseUsers);
                    break;

                case DocumentRepositoryRequest.StorageAction.RemoveCaseUser:
                    RemoveCaseUser(web, caseFolder, request.CaseUsers);
                    break;

                case DocumentRepositoryRequest.StorageAction.CloseCase:
                    CloseCase(web, caseFolder, request.ServiceGroup);
                    break;
            }
            ApproveCaseFolder(caseFolder);
        }

        /// <summary>
        /// This methods adds the users to an existing case folder
        /// conditions:
        /// if user is external, and the current library is not externaldocuments, skip the loop
        /// if user is internal, and the current library is external, add users who are approvers as Administrator
        /// else add everyone as Contributors
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        private void AddCaseUser(SPWeb web, SPFolder caseFolder, IEnumerable<DocumentRepositoryCaseUser> users)
        {
            //If the case is closed and they want to reopen it = Error!
            var usersOnThisCase = caseFolder.Item.RoleAssignments;
            var lob = caseFolder.Item.ParentList.Title.Split('_')[0];
            var countingGroups = 0;

            foreach (var user in usersOnThisCase.Cast<SPRoleAssignment>())
            {
                if (!user.Member.Name.Equals(string.Format("{0}{1}{2}", lob, Constants.DocumentListLibraries.cLobSeperator, Constants.Permissions.closedCaseAccess))
                    && !user.Member.Name.Equals(web.AssociatedOwnerGroup.Name))
                {
                    countingGroups = countingGroups + 1;
                }
            }

            if (countingGroups == 0) //Only closedCaseAccess is there ergo it's a closed case
            {
                this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                    "AddCaseUser/OpenCase",
                    String.Format("Error at AddCaseUser/OpenCase on '{0}' with case folder value {1} : Case has been closed, re-opening it is not allowed", this.GetType().Name, caseFolder.Name)));
                return;
            }

            //From here on it would be the normal AddCaseUser flow

            if (!caseFolder.Item.HasUniqueRoleAssignments)
            {
                caseFolder.Item.BreakRoleInheritance(true);
            }

            foreach (var user in users)
            {
                try
                {
                    var thisListType = caseFolder.Item.ParentList.Title.Split('_')[1];

                    if (user.UserType.Equals(DocumentRepositoryCaseUser.CaseUserType.External) &&
                        !thisListType.Equals(Constants.DocumentListLibraries.cExternalDocuments))
                        continue;

                    var spUser = web.EnsureUser(ReturnClaimsUserName(user.UserName, user.UserType));
                    var spRoleDefinition = web.RoleDefinitions.GetByType(SPRoleType.Contributor);

                    if (user.UserType.Equals(DocumentRepositoryCaseUser.CaseUserType.Internal) &&
                        thisListType.Equals(Constants.DocumentListLibraries.cExternalDocuments) &&
                        user.IsApprover)
                        spRoleDefinition = web.RoleDefinitions.GetByType(SPRoleType.Administrator);

                    var oSpRoleAssignment = new SPRoleAssignment(spUser);
                    oSpRoleAssignment.RoleDefinitionBindings.Add(spRoleDefinition);
                    caseFolder.Item.RoleAssignments.Add(oSpRoleAssignment);

                    web.AssociatedMemberGroup.AddUser(spUser);
                }
                catch (Exception e)
                {
                    this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                        "AddCaseUser",
                        String.Format("Error at AddCaseUser on '{0}' with case value {1} and user value {2} : {3}, Stack trace : {4}", this.GetType().Name, caseFolder.Name, user.UserName, e.Message, e.StackTrace)));
                }
            }
        }

        /// <summary>
        /// Adds an exception to the validation errors.
        /// </summary>
        /// <param name="processingPoint">The processing point.</param>
        /// <param name="value">The value.</param>
        /// <param name="genEx">The gen ex.</param>
        private void AddException(string processingPoint, string value, Exception genEx)
        {
            this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                processingPoint,
                String.Format("Error at {0} on '{1}' with value '{2}': {3}, Stack trace:\r\n{4}", processingPoint, this.GetType().Name, value, genEx.Message, genEx.StackTrace)));
        }

        /// <summary>
        /// Look for Case Folder in web with caseName
        /// if found, return
        /// if not found, create a new folder, return
        /// </summary>
        /// <param name="web"></param>
        /// <param name="libName"></param>
        /// <param name="caseName"></param>
        /// <returns>caseFolder</returns>
        private SPFolder AddOrFindCase(SPWeb web, string libName, string caseName)
        {
            try
            {
                var library = web.Lists.TryGetList(libName);
                if (library == null)
                {
                    this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                        "AddOrFindCase",
                        String.Format("Error at AddOrFindCase on '{0}' with library value {1} : No such library", this.GetType().Name, libName)));
                    return null;
                }

                var isSharePointGenericList = library.BaseTemplate.Equals(SPListTemplateType.GenericList);

                var caseFolderPath = isSharePointGenericList
                                            ? string.Format("Lists/{0}/{1}", libName, caseName)
                                            : libName + "/" + caseName;

                if (!web.GetFolder(caseFolderPath).Exists)
                {
                    if (isSharePointGenericList)
                    {
                        var folderItem = library.Items.Add(library.RootFolder.ServerRelativeUrl + string.Empty, SPFileSystemObjectType.Folder);
                        folderItem[Constants.DocumentListLibraries.cCaseNoteFolder] = caseName;
                        folderItem[Constants.DocumentListLibraries.cCaseNoteFolderTitle] = caseName;
                        folderItem.Update();
                    }
                    else
                    {
                        web.Folders.Add(caseFolderPath);
                    }
                }
                return web.GetFolder(caseFolderPath);
            }
            catch (Exception e)
            {
                this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                    "AddOrFindCase",
                    String.Format("Error at AddOrFindCase on '{0}' with library value {1} and case value {2} : {3}, Stack trace : {4}", this.GetType().Name, libName, caseName, e.Message, e.StackTrace)));
                return null;
            }
        }

        /// <summary>
        /// Change Approval status of folder to Approved
        /// </summary>
        /// <param name="caseFolder"></param>
        private void ApproveCaseFolder(SPFolder caseFolder)
        {
            try
            {
                if (!caseFolder.Item.ParentList.EnableModeration)
                {
                    return;
                }
                if (null == caseFolder.Item.ModerationInformation)
                {
                    this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                        "ApproveCaseFolder",
                        String.Format("Error at ApproveCaseFolder on '{0}' with case folder value {1} : No moderation information found", this.GetType().Name, caseFolder.Name)));
                    return;
                }
                caseFolder.Item.ModerationInformation.Status = SPModerationStatusType.Approved;
                caseFolder.Item.ModerationInformation.Comment = string.Empty;
                caseFolder.Item.Update();
            }
            catch (Exception e)
            {
                this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                    "ApproveCaseFolder",
                    String.Format("Error at ApproveCaseFolder on '{0}' with value {1} : {2}, Stack trace: {3}", this.GetType().Name, caseFolder, e.Message, e.StackTrace)));
            }
        }

        /// <summary>
        /// This method closes the case.
        /// Removes all permission-roles of all users tied to a case folder
        /// Adds Reader permission-role to all users
        /// Except owner's group which remains Administrator
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        private void CloseCase(SPWeb web, SPFolder caseFolder, string lob)
        {
            int userIndexForDebuggingPurposes = 0;
            try
            {
                if (!caseFolder.Item.HasUniqueRoleAssignments)
                {
                    caseFolder.Item.BreakRoleInheritance(true);
                }

                var rolesOnThisList = caseFolder.Item.RoleAssignments;

                for (var userIndex = rolesOnThisList.Count - 1; userIndex >= 0; userIndex--)
                {
                    userIndexForDebuggingPurposes = userIndex;
                    caseFolder.Item.RoleAssignments.Remove(userIndex);
                }

                caseFolder.Item.Update();

            }
            catch (Exception e)
            {
                this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                    "CloseCase",
                    String.Format("Error at CloseCase on '{0}' on folder {1} on userIndex {2}: {3}, Stack trace : {4}", this.GetType().Name, caseFolder.Name, userIndexForDebuggingPurposes, e.Message, e.StackTrace)));
            }

            try
            {
               
                var ownersRoleDefinition = caseFolder.Item.ParentList.ParentWeb.RoleDefinitions.GetByType(SPRoleType.Administrator);
                var ownersRoleAssignment = new SPRoleAssignment(caseFolder.Item.ParentList.ParentWeb.AssociatedOwnerGroup);
                ownersRoleAssignment.RoleDefinitionBindings.Add(ownersRoleDefinition);
                caseFolder.Item.RoleAssignments.Add(ownersRoleAssignment);
                caseFolder.Item.Update();

            }
            catch (Exception e)
            {
                this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                    "CloseCase",
                    String.Format("Error at CloseCase on '{0}' on folder {1} : {2} ({3}), Stack trace : {4}", this.GetType().Name, caseFolder.Name, e.Message, string.Format("{0}{1}{2}", lob, Constants.DocumentListLibraries.cLobSeperator, Constants.Permissions.closedCaseAccess), e.StackTrace)));
            }

            try
            {
                
                var closedCaseRoleDefinition = caseFolder.Item.ParentList.ParentWeb.RoleDefinitions.GetByType(SPRoleType.Reader);
                var group = caseFolder.Item.ParentList.ParentWeb.Groups.GetByName(string.Format("{0}{1}{2}", lob, Constants.DocumentListLibraries.cLobSeperator, Constants.Permissions.closedCaseAccess));
                var uniqueRoleAssignment = new SPRoleAssignment(group);
                uniqueRoleAssignment.RoleDefinitionBindings.Add(closedCaseRoleDefinition);
                caseFolder.Item.RoleAssignments.Add(uniqueRoleAssignment);
                caseFolder.Item.Update();
            }
            catch (Exception e)
            {
                this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                    "CloseCase",
                    String.Format("Error at CloseCase on '{0}' on folder {1} : {2} ({3}), Stack trace : {4}", this.GetType().Name, caseFolder.Name, e.Message, string.Format("{0}{1}{2}", lob, Constants.DocumentListLibraries.cLobSeperator, Constants.Permissions.closedCaseAccess), e.StackTrace)));
            }

        }

        /// <summary>
        /// This methods removes the users from an existing case.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        private void RemoveCaseUser(SPWeb web, SPFolder caseFolder, IEnumerable<DocumentRepositoryCaseUser> users)
        {

            if (!caseFolder.Item.HasUniqueRoleAssignments)
            {
                caseFolder.Item.BreakRoleInheritance(true);
            }

            var usersOnThisCase = caseFolder.Item.RoleAssignments;

            foreach (var principal in users.Select(user => web.EnsureUser(ReturnClaimsUserName(user.UserName, user.UserType))))
            {
                try
                {
                    usersOnThisCase.Remove(principal);
                }
                catch (Exception e)
                {
                    this.Results.ValidationResults.Errors.Add(new FluentValidation.Results.ValidationFailure(
                        "RemoveCaseUser",
                        String.Format("Error at RemoveCaseUser on '{0}' with value {1} : {2}, Stack trace : {3}", this.GetType().Name, principal, e.Message, e.StackTrace)));
                }
            }
        }

        /// <summary>
        /// Return corresponding claims token of user, depending on user type
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="userType"></param>
        /// <returns></returns>
        private string ReturnClaimsUserName(string userName, DocumentRepositoryCaseUser.CaseUserType userType)
        {
            return userType.Equals(DocumentRepositoryCaseUser.CaseUserType.External)
                       ? Constants.Permissions.cUniqueClaimType + Constants.Permissions.cPortalGuardTokenPrefix + userName
                       : Constants.Permissions.cStandardWindowsClaim + userName;
        }
    }
}
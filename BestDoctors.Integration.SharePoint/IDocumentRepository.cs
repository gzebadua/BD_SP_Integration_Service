using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using BestDoctors.Integration.Schemas.Shared.Requests;

namespace BestDoctors.Integration.SharePoint
{
    [ServiceContract]
    public interface IDocumentRepository
    {
        /// <summary>
        /// Submits the specified request.
        /// </summary>
        /// <param name="request">This is the request.</param>
        /// <returns></returns>
        [OperationContract]
        DocumentRepositoryRequest Submit(DocumentRepositoryRequest request);
    }
}
///-----------------------------------------------------------------
///   File:         FetchWorker.cs
///   Author:   	Andre Laskawy           
///   Date:         02.10.2018 19:46:56
///-----------------------------------------------------------------

namespace Nanomite.Server.Authenticaton.Worker
{
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using Nanomite.Core.DataAccess;
    using Nanomite.Core.Network.Common;
    using Nanomite.Core.Server.Base.Worker;
    using Nanomite.Server.Authenticaton.Handler;
    using NLog;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="FetchWorker" />
    /// </summary>
    public class FetchWorker : CommonFetchWorker
    {
        /// <summary>
        /// The broker identifier
        /// </summary>
        private string brokerId;

        /// <summary>
        /// The authentication handler
        /// </summary>
        private AuthenticationHandler authenticationHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="FetchWorker"/> class.
        /// </summary>
        /// <param name="brokerId">The broker identifier.</param>
        /// <param name="secret">The secret.</param>
        public FetchWorker(string brokerId, string secret) : base()
        {
            this.brokerId = brokerId;
            this.authenticationHandler = new AuthenticationHandler(secret);
        }

        /// <summary>
        /// Processes the fetch request
        /// </summary>
        /// <param name="req">The req<see cref="FetchRequest" /></param>
        /// <param name="streamId">The streamId<see cref="string" /></param>
        /// <param name="token">The token<see cref="string" /></param>
        /// <param name="header">The header<see cref="Metadata" /></param>
        /// <param name="checkForAuthentication">if set to <c>true</c> [check for authentication].</param>
        /// <returns>The <see cref="Task{GrpcResponse}"/></returns>
        public override async Task<GrpcResponse> ProcessFetch(FetchRequest req, string streamId, string token, Metadata header, bool checkForAuthentication = true)
        {
            try
            {
                if (!this.authenticationHandler.TokenIsValid(token))
                {
                    return Unauthorized();
                }

                if (string.IsNullOrEmpty(req.TypeDescription)
                    || req.TypeDescription != Any.Pack(new NetworkUser()).TypeUrl)
                {
                    throw new Exception("invalid modeltype for fetch request");
                }

                Log("Processing fetch request for type: " + req.TypeDescription, LogLevel.Trace);

                System.Type type = CommonRepositoryHandler.GetAllRepositories().FirstOrDefault(p => p.ProtoTypeUrl == req.TypeDescription).ModelType;
                bool includeAll = req.InlcudeRelatedEntities;
                string query = req.Query;

                var result = CommonRepositoryHandler.GetListByQuery(type, query, includeAll).Cast<IMessage>();
                return await Task.FromResult(Ok(result.ToList()));
            }
            catch (Exception ex)
            {
                Log("Fetch error", ex);
                return BadRequest(ex);
            }
        }
    }
}

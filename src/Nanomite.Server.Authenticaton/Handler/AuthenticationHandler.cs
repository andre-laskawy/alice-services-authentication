///-----------------------------------------------------------------
///   File:         AuthenticationHandler.cs
///   Author:   	Andre Laskawy           
///   Date:         06.10.2018 14:27:40
///-----------------------------------------------------------------

namespace Nanomite.Server.Authenticaton.Handler
{
    using Google.Protobuf.WellKnownTypes;
    using Nanomite.Core.Authentication;
    using Nanomite.Core.DataAccess;
    using Nanomite.Core.Network.Common;
    using Nanomite.Core.Network.Common.Models;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="AuthenticationHandler" />
    /// </summary>
    public class AuthenticationHandler
    {
        /// <summary>
        /// The handler
        /// </summary>
        private CommonJwtTokenAuthenticationHandler handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationHandler"/> class.
        /// </summary>
        /// <param name="secret">The secret.</param>
        public AuthenticationHandler(string secret)
        {
            this.handler = new CommonJwtTokenAuthenticationHandler(GetUser, secret);
        }

        /// <summary>
        /// Handles the authentication command.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <returns>a grpc response</returns>
        public async Task<GrpcResponse> HandleCmd(Command cmd)
        {
            GrpcResponse result = null;

            if (cmd.Topic == StaticCommandKeys.Connect)
            {
                try
                {
                    var user = cmd.Data.FirstOrDefault().CastToModel<NetworkUser>();
                    if (user == null)
                    {
                        throw new Exception("Ivalid authentication data.");
                    }

                    if (!string.IsNullOrEmpty(user.AuthenticationToken))
                    {
                        if (!this.handler.Authenticate(user.AuthenticationToken))
                        {
                            throw new Exception("Invalid token");
                        }
                    }
                    else
                    {
                        user.AuthenticationToken = await this.handler.Authenticate(user.LoginName, user.PasswordHash);
                        if (string.IsNullOrEmpty(user.AuthenticationToken))
                        {
                            throw new Exception("Invalid user or password");
                        }
                    }

                    result = new GrpcResponse() { Result = ResultCode.Ok };
                    result.Data.Add(Any.Pack(user));
                }
                catch (Exception ex)
                {
                    // Todo logging
                    Console.WriteLine(ex);
                    result = new GrpcResponse() { Result = ResultCode.Ok };
                    result.Data.Add(Any.Pack(ex.ToProtoException()));
                }

                return result;
            }


            return new GrpcResponse() { Result = ResultCode.Ok, Message = "Invalid Topic" };
        }

        /// <summary>
        /// Is token valid.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        public bool TokenIsValid(string token)
        {
            return this.handler.Authenticate(token);
        }

        /// <summary>
        /// Gets the user from the databas
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="passwordHash">The password hash.</param>
        /// <returns>the users token</returns>
        private async Task<string> GetUser(string user, string passwordHash)
        {
            string filter = string.Format("LoginName eq '{0}' and passwordHash eq '{1}'", user, passwordHash);
            var result = CommonRepositoryHandler.GetListByQuery(typeof(NetworkUser), filter, false).Cast<NetworkUser>().FirstOrDefault();
            return await Task.FromResult(result?.AuthenticationToken);
        }
    }
}

///-----------------------------------------------------------------
///   File:         ActionWorker.cs
///   Author:   	Andre Laskawy           
///   Date:         02.10.2018 19:45:39
///-----------------------------------------------------------------

namespace Nanomite.Server.Authenticaton.Worker
{
    using Grpc.Core;
    using Nanomite.Core.Network.Common;
    using Nanomite.Core.Server.Base.Handler;
    using Nanomite.Core.Server.Base.Worker;
    using Nanomite.Server.Authenticaton.Handler;
    using NLog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="ActionWorker" />
    /// </summary>
    public class ActionWorker : CommonActionWorker
    {
        /// <summary>
        /// The authentication handler
        /// </summary>
        private AuthenticationHandler authenticationHandler;

        /// <summary>
        /// Gets or sets a value indicating whether the cloud is ready for a client connection.
        /// </summary>
        public bool ReadyForConnections { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionWorker"/> class.
        /// </summary>
        /// <param name="brokerId">The broker identifier.</param>
        /// <param name="secret">The secret.</param>
        public ActionWorker(string brokerId, string secret) : base()
        {
            this.ReadyForConnections = false;
            this.authenticationHandler = new AuthenticationHandler(secret);
        }

        /// <inheritdoc />
        public override async Task<GrpcResponse> StreamConnected(IStream<Command> stream, string token, Metadata header)
        {
            CommonBaseHandler.Log(this.ToString(), string.Format("Client stream {0} is connected.", stream.Id), LogLevel.Info);
            return await Task.FromResult(Ok());
        }

        /// <inheritdoc />
        public override async Task<GrpcResponse> StreamDisconnected(string streamID)
        {
            CommonBaseHandler.Log(this.ToString(), string.Format("Client stream {0} disconnted.", streamID), LogLevel.Info);
            return await Task.FromResult(Ok());
        }

        /// <inheritdoc />
        public override async Task<GrpcResponse> ProcessCommand(string cloudId, Command cmd, string streamId, string token, Metadata header, bool checkForAuthentication = true)
        {
            while (!this.ReadyForConnections)
            {
                await Task.Delay(1);
            }

            try
            {
                Log("Begin command: " + cmd.Topic, LogLevel.Debug);
                return await this.authenticationHandler.HandleCmd(cmd);
            }
            catch (Exception ex)
            {
                Log("Command Error " + cmd.Topic, ex);
                return BadRequest(ex);
            }
            finally
            {
                Log("End Command: " + cmd.Topic, LogLevel.Debug);
            }
        }
    }
}

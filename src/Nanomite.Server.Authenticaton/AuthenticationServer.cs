///-----------------------------------------------------------------
///   File:         AuthenticationServer.cs
///   Author:   	Andre Laskawy           
///   Date:         02.10.2018 19:56:42
///-----------------------------------------------------------------

namespace Nanomite.Server.Authenticaton
{
    using Nanomite.Common.Common.Services.GrpcService;
    using Nanomite.MessageBroker.Helper;
    using Nanomite.Core.Network.Common;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Nanomite.Core.Server.Base.Broker;
    using Nanomite.Core.Server.Base.Handler;
    using Nanomite.Core.Server;
    using Nanomite.Core.Server.Base.Locator;
    using Nanomite.Core.Network;
    using Nanomite.Core.Server.Base;
    using Nanomite.Server.Authenticaton.Worker;
    using Nanomite.Core.DataAccess;
    using Nanomite.Core.DataAccess.Database;
    using Nanomite.Server.Authenticaton.Data.Database;

    /// <summary>
    /// Defines the <see cref="AuthenticationServer" />
    /// </summary>
    public class AuthenticationServer : BaseBroker
    {
        /// <summary>
        /// Mains the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            // start cloud
            Cloud.Run();
        }

        /// <inheritdoc />
        public override async Task Start(IConfig config)
        {
            // config
            ConfigHelper c = config as ConfigHelper;

            // database
            BaseContext.Setup<UserContext>();
            await CheckDefaultUser(c.DefaultUser, c.DefaultUserPass, c.Secret);

            // get endpoint for local grpc host
            IPEndPoint grpcEndPoint = new IPEndPoint(IPAddress.Any, config.PortGrpc);//GetCloudAddress(config);

            // get endpoint which will be repoted to the network
            IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
            var host = entry.AddressList.LastOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork);
            var upnpEndpoint = new IPEndPoint(host, CloudLocator.GetConfig().PortGrpc);

            // Start the server grpc endpoints
            CommonBaseHandler.Log(this.ToString(), "GRPC IP ADRESS: " + upnpEndpoint, NLog.LogLevel.Info);
            StartGrpcServer(grpcEndPoint, config.SrcDeviceId);

            // accept client connections
            (this.ActionWorker as ActionWorker).ReadyForConnections = true;
            Console.WriteLine("Cloud started -> ready for connections.");
        }

        /// <inheritdoc />
        public override void Register()
        {
            CloudLocator.GetCloud = (() =>
            {
                return this;
            });

            CloudLocator.GetConfig = (() =>
            {
                return new ConfigHelper();
            });

            // config
            var config = CloudLocator.GetConfig() as ConfigHelper;

            // Workers to handle messages (commands /fetch / messages)
            this.ActionWorker = new ActionWorker(config.SrcDeviceId, config.Secret);
            this.FetchWorker = new FetchWorker(config.SrcDeviceId, config.Secret);
        }

        /// <inheritdoc />
        public override void AddMiddlewares(dynamic app, dynamic env)
        {
        }

        /// <inheritdoc />
        public override void AddServices(dynamic servicCollection)
        {
        }

        /// <summary>
        /// Starts the GRPC server.
        /// </summary>
        /// <param name="endpoint">The <see cref="IPEndPoint" /></param>
        /// <param name="srcDeviceId">The <see cref="string" /></param>
        private void StartGrpcServer(IPEndPoint endpoint, string srcDeviceId)
        {
            // GRPC server
            IServer<Command, FetchRequest, GrpcResponse> communicationServiceGrpc = GRPCServer.Create(endpoint, null, null);
            StartServer(srcDeviceId, communicationServiceGrpc);
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="srcDeviceId">The source device identifier.</param>
        /// <param name="communicationService">The communication service.</param>
        private void StartServer(string srcDeviceId, IServer<Command, FetchRequest, GrpcResponse> communicationService)
        {
            // Grpc server
            CommunicationServer server = new CommunicationServer(communicationService, srcDeviceId);

            // Grpc server actions
            server.OnAction = async (cmd, streamId, token, header) => { return await ActionWorker.ProcessCommand(srcDeviceId, cmd, streamId, token, header); };
            server.OnFetch = async (request, streamId, token, header) => { return await FetchWorker.ProcessFetch(request, streamId, token, header); };
            server.OnStreamOpened = async (stream, token, header) => { return await ActionWorker.StreamConnected(stream, token, header); };
            server.OnStreaming = async (cmd, stream, token, header) =>
            {
                return await ActionWorker.ProcessCommand(srcDeviceId, cmd, stream.Id, token, header);
            };
            server.OnClientDisconnected = async (id) =>
            {
                await CommonSubscriptionHandler.UnregisterStream(id);
            };
            server.OnLog += (sender, srcid, msg, level) =>
            {
                CommonBaseHandler.Log(sender.ToString(), msg, level);
            };
            server.Start();
        }

        /// <summary>
        /// Checks the default user.
        /// </summary>
        /// <param name="loginName">Name of the login.</param>
        /// <param name="pass">The pass.</param>
        /// <param name="secret">The secret.</param>
        /// <returns></returns>
        private async Task<NetworkUser> CheckDefaultUser(string loginName, string pass, string secret)
        {
            try
            {
                string filter = nameof(NetworkUser.LoginName) + " eq '" + loginName + "'";
                var user = CommonRepositoryHandler.GetListByQuery(typeof(NetworkUser), filter, false).Cast<NetworkUser>().FirstOrDefault();

                if (user == null)
                {
                    user = new NetworkUser()
                    {
                        IsActive = true,
                        IsAdmin = true,
                        LoginName = loginName,
                        Name = loginName,
                        PasswordHash = pass.Hash(secret),
                        CreatedDT = DateTime.UtcNow.ToISO8601(),
                        ModifiedDT = DateTime.UtcNow.ToISO8601()
                    };
                    await CommonRepositoryHandler.CreateData(typeof(NetworkUser), user);
                }

                return user;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create default User: " + ex.Message);
            }
        }
    }
}

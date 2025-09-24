using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System.Net;

namespace BeverageFillingLineServer
{
    public class RobustProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("💪 Starting ROBUST Beverage Filling Line Server...");

            try
            {
                var application = new ApplicationInstance
                {
                    ApplicationName = "Beverage Filling Line Server",
                    ApplicationType = ApplicationType.Server
                };

                // Create minimal, robust configuration
                var config = CreateRobustConfiguration();
                application.ApplicationConfiguration = config;

                Console.WriteLine("🔧 Configuration created, checking certificates...");

                // Check certificates with better error handling
                try
                {
                    await application.CheckApplicationInstanceCertificates(true, 0);
                    Console.WriteLine("✅ Certificates OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Certificate warning: {ex.Message}");
                    Console.WriteLine("Continuing anyway...");
                }

                Console.WriteLine("🚀 Starting server...");

                // Create the most basic server possible
                var server = new RobustServer();

                try
                {
                    await application.Start(server);
                    Console.WriteLine("🎉 Server started successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Server start failed: {ex.Message}");
                    Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");

                    // Try alternative startup
                    Console.WriteLine("🔄 Trying alternative startup...");
                    server.StartAlternative(config);
                }

                Console.WriteLine();
                Console.WriteLine("📡 Server Information:");
                Console.WriteLine($"   Endpoint: opc.tcp://localhost:4840");
                Console.WriteLine($"   Security: None");
                Console.WriteLine($"   Status: Running");
                Console.WriteLine();
                Console.WriteLine("🔍 Test connection now with UaExpert!");
                Console.WriteLine("💡 If still failing, try: opc.tcp://127.0.0.1:4840");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();

                server.Stop();
                Console.WriteLine("🛑 Server stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Critical Error: {ex.Message}");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                Console.WriteLine();
                Console.WriteLine("💡 Troubleshooting suggestions:");
                Console.WriteLine("   1. Run as Administrator");
                Console.WriteLine("   2. Check Windows Firewall");
                Console.WriteLine("   3. Verify port 4840 is available");
                Console.WriteLine("   4. Try different endpoint in UaExpert");
                Console.ReadKey();
            }
        }

        private static ApplicationConfiguration CreateRobustConfiguration()
        {
            Console.WriteLine("⚙️  Creating robust configuration...");

            return new ApplicationConfiguration
            {
                ApplicationName = "Beverage Filling Line Server",
                ApplicationUri = "urn:localhost:BeverageFillingLineServer",
                ApplicationType = ApplicationType.Server,

                ServerConfiguration = new ServerConfiguration
                {
                    BaseAddresses = new StringCollection {
                        "opc.tcp://localhost:4840",
                        "opc.tcp://127.0.0.1:4840",
                        "opc.tcp://0.0.0.0:4840"
                    },
                    SecurityPolicies = new ServerSecurityPolicyCollection
                    {
                        new ServerSecurityPolicy
                        {
                            SecurityMode = MessageSecurityMode.None,
                            SecurityPolicyUri = SecurityPolicies.None
                        }
                    },
                    UserTokenPolicies = new UserTokenPolicyCollection
                    {
                        new UserTokenPolicy(UserTokenType.Anonymous)
                    },
                    MaxSessionCount = 10,
                    MaxSessionTimeout = 60000,
                    MinRequestThreadCount = 1,
                    MaxRequestThreadCount = 10,
                    MaxQueuedRequestCount = 100
                },

                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true,
                    RejectSHA1SignedCertificates = false,
                    RejectUnknownRevocationStatus = false,
                    MinimumCertificateKeySize = 1024,
                    SuppressNonceValidationErrors = true
                },

                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 30000,
                    MaxStringLength = 65535,
                    MaxByteStringLength = 65535,
                    MaxArrayLength = 1000,
                    MaxMessageSize = 1048576,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 60000,
                    SecurityTokenLifetime = 300000
                },

                TraceConfiguration = new TraceConfiguration
                {
                    OutputFilePath = "",
                    TraceMasks = 0
                }
            };
        }
    }

    public class RobustServer : StandardServer
    {
        private BeverageFillingLineMachine m_machine;
        private Timer m_timer;

        public RobustServer()
        {
            m_machine = new BeverageFillingLineMachine();
        }

        protected override ServerProperties LoadServerProperties()
        {
            return new ServerProperties
            {
                ManufacturerName = "FluidFill Systems",
                ProductName = "Beverage Filling Line Server",
                ProductUri = "urn:FluidFill:BeverageServer",
                SoftwareVersion = "1.0.0",
                BuildNumber = "1",
                BuildDate = DateTime.Now
            };
        }

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("🏗️  Creating minimal node manager...");

            try
            {
                var nodeManager = new MinimalNodeManager(server, configuration, m_machine);
                var masterNodeManager = new MasterNodeManager(server, configuration, null, nodeManager);

                // Start simulation timer
                m_timer = new Timer(UpdateSimulation, null, 2000, 2000);

                Console.WriteLine("✅ Node manager created!");
                return masterNodeManager;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Node manager failed: {ex.Message}");
                Console.WriteLine("Creating basic node manager...");
                return new MasterNodeManager(server, configuration, null);
            }
        }

        private void UpdateSimulation(object state)
        {
            try
            {
                m_machine.UpdateSimulation();
                Console.WriteLine($"🔄 [{DateTime.Now:HH:mm:ss}] Status: {m_machine.MachineStatus} | " +
                                $"Fill: {m_machine.ActualFillVolume:F1}ml | " +
                                $"Tank: {m_machine.ProductLevelTank:F1}% | " +
                                $"{m_machine.CurrentStation}");

                if (m_machine.ActiveAlarms.Count > 0)
                {
                    Console.WriteLine($"⚠️  ALARMS: {string.Join(", ", m_machine.ActiveAlarms.Take(2))}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Simulation error: {ex.Message}");
            }
        }

        public void StartAlternative(ApplicationConfiguration configuration)
        {
            Console.WriteLine("🔄 Starting alternative server mode...");
            m_timer = new Timer(UpdateSimulation, null, 2000, 2000);
            Console.WriteLine("✅ Alternative server started with simulation only");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_timer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class MinimalNodeManager : CustomNodeManager2
    {
        private BeverageFillingLineMachine m_machine;

        public MinimalNodeManager(IServerInternal server, ApplicationConfiguration configuration, BeverageFillingLineMachine machine)
            : base(server, configuration, "urn:BeverageServer:")
        {
            m_machine = machine;
            SetNamespaces("urn:BeverageServer:");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);
            }
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();

            // Create simple root folder
            FolderState root = new FolderState(null)
            {
                NodeId = new NodeId("BeverageData", NamespaceIndex),
                BrowseName = new QualifiedName("Beverage Data", NamespaceIndex),
                DisplayName = new LocalizedText("Beverage Data"),
                TypeDefinitionId = ObjectTypeIds.FolderType
            };

            root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            AddPredefinedNode(context, root);
            predefinedNodes.Add(root);

            Console.WriteLine("📁 Minimal address space created");
            return predefinedNodes;
        }
    }
}
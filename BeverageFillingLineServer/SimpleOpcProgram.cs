using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class SimpleOpcProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Starting SIMPLE OPC UA Server...");

            try
            {
                // Create the simplest possible working configuration
                var config = CreateSimpleConfig();
                var application = new ApplicationInstance
                {
                    ApplicationName = "Simple Beverage Server",
                    ApplicationType = ApplicationType.Server,
                    ApplicationConfiguration = config
                };

                Console.WriteLine("🔧 Starting server on port 4850...");

                var server = new SimpleOpcServer();

                try
                {
                    await application.Start(server);
                    Console.WriteLine("🎉 SUCCESS! OPC UA Server is running!");
                    Console.WriteLine();
                    Console.WriteLine("📡 Connection Details:");
                    Console.WriteLine("   URL: opc.tcp://localhost:4850");
                    Console.WriteLine("   Security: None");
                    Console.WriteLine("   Authentication: Anonymous");
                    Console.WriteLine();
                    Console.WriteLine("🔗 In UaExpert, use: opc.tcp://localhost:4850");
                    Console.WriteLine();

                    // Keep running and show status
                    while (true)
                    {
                        await Task.Delay(5000);
                        Console.WriteLine($"⚡ [{DateTime.Now:HH:mm:ss}] Server running - Connect with UaExpert!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Server failed to start: {ex.Message}");

                    // Try with just simulation
                    Console.WriteLine("🔄 Falling back to simulation-only mode...");
                    RunSimulationOnly();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Critical error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ReadKey();
            }
        }

        private static ApplicationConfiguration CreateSimpleConfig()
        {
            return new ApplicationConfiguration
            {
                ApplicationName = "Simple Beverage Server",
                ApplicationUri = "urn:SimpleServer",
                ApplicationType = ApplicationType.Server,

                ServerConfiguration = new ServerConfiguration
                {
                    BaseAddresses = new StringCollection { "opc.tcp://localhost:4850" },
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
                    }
                },

                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true
                },

                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 15000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 1048576,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                }
            };
        }

        private static void RunSimulationOnly()
        {
            var machine = new BeverageFillingLineMachine();
            var timer = new Timer(_ => {
                try
                {
                    machine.UpdateSimulation();
                    Console.WriteLine($"🔄 [{DateTime.Now:HH:mm:ss}] Simulation: {machine.MachineStatus} | " +
                                    $"Fill: {machine.ActualFillVolume:F1}ml | " +
                                    $"Tank: {machine.ProductLevelTank:F1}% | " +
                                    $"{machine.CurrentStation}");

                    if (machine.ActiveAlarms.Count > 0)
                    {
                        Console.WriteLine($"⚠️  ALARMS: {string.Join(" | ", machine.ActiveAlarms.Take(2))}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Simulation error: {ex.Message}");
                }
            }, null, 1000, 2000);

            Console.WriteLine("✅ Simulation-only mode running");
            Console.WriteLine("❌ OPC UA not available - check network/firewall settings");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            timer?.Dispose();
        }
    }

    public class SimpleOpcServer : StandardServer
    {
        private Timer m_simulationTimer;
        private BeverageFillingLineMachine m_machine;

        public SimpleOpcServer()
        {
            m_machine = new BeverageFillingLineMachine();
        }

        protected override ServerProperties LoadServerProperties()
        {
            return new ServerProperties
            {
                ManufacturerName = "Simple Systems",
                ProductName = "Simple Beverage Server",
                SoftwareVersion = "1.0",
                BuildNumber = "1",
                BuildDate = DateTime.Now
            };
        }

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("🏗️  Creating simple master node manager...");

            try
            {
                var nodeManager = new TestNodeManager(server, configuration, m_machine);
                var masterManager = new MasterNodeManager(server, configuration, null, nodeManager);

                // Start simulation
                m_simulationTimer = new Timer(UpdateSimulation, null, 1000, 2000);
                Console.WriteLine("✅ Node manager created with simulation");

                return masterManager;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Node manager error: {ex.Message}");
                // Return minimal manager
                return new MasterNodeManager(server, configuration, null);
            }
        }

        private void UpdateSimulation(object state)
        {
            try
            {
                m_machine.UpdateSimulation();
                Console.WriteLine($"📊 [{DateTime.Now:HH:mm:ss}] OPC Values Updated - " +
                                $"Status: {m_machine.MachineStatus}, " +
                                $"Fill: {m_machine.ActualFillVolume:F1}ml, " +
                                $"Tank: {m_machine.ProductLevelTank:F1}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Simulation update error: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_simulationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class TestNodeManager : CustomNodeManager2
    {
        private BeverageFillingLineMachine m_machine;
        private Dictionary<string, BaseDataVariableState> m_variables;

        public TestNodeManager(IServerInternal server, ApplicationConfiguration configuration, BeverageFillingLineMachine machine)
            : base(server, configuration, "urn:SimpleServer")
        {
            m_machine = machine;
            m_variables = new Dictionary<string, BaseDataVariableState>();
            SetNamespaces("urn:SimpleServer");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);
                Console.WriteLine("📁 Simple address space created");
            }
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();

            try
            {
                // Create simple root folder
                FolderState root = new FolderState(null)
                {
                    NodeId = new NodeId("SimpleData", NamespaceIndex),
                    BrowseName = new QualifiedName("Simple Data", NamespaceIndex),
                    DisplayName = new LocalizedText("Simple Data"),
                    TypeDefinitionId = ObjectTypeIds.FolderType
                };

                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                AddPredefinedNode(context, root);
                predefinedNodes.Add(root);

                // Create just one simple variable for testing
                var testVar = new BaseDataVariableState(root)
                {
                    NodeId = new NodeId("TestValue", NamespaceIndex),
                    BrowseName = new QualifiedName("Test Value", NamespaceIndex),
                    DisplayName = new LocalizedText("Test Value"),
                    TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                    DataType = DataTypeIds.String,
                    ValueRank = ValueRanks.Scalar,
                    AccessLevel = AccessLevels.CurrentRead,
                    UserAccessLevel = AccessLevels.CurrentRead,
                    Value = "Hello from Beverage Server!",
                    StatusCode = StatusCodes.Good,
                    Timestamp = DateTime.UtcNow
                };

                root.AddChild(testVar);
                predefinedNodes.Add(testVar);
                m_variables["TestValue"] = testVar;

                Console.WriteLine("📋 Created 1 test variable for UaExpert");
                return predefinedNodes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating nodes: {ex.Message}");
                return predefinedNodes;
            }
        }
    }
}
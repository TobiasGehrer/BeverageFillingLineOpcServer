using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class WorkingFinalProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🎯 Starting WORKING FINAL Beverage Filling Line Server...");

            // Try different ports to avoid conflicts
            int[] ports = { 4841, 4842, 4843, 48400, 49840 };

            foreach (int port in ports)
            {
                Console.WriteLine($"🔍 Trying port {port}...");

                if (await TryStartServer(port))
                {
                    Console.WriteLine($"✅ SUCCESS! Server running on port {port}");
                    Console.WriteLine();
                    Console.WriteLine($"🌐 Connect with UaExpert using: opc.tcp://localhost:{port}");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
                else
                {
                    Console.WriteLine($"❌ Port {port} failed, trying next...");
                }
            }

            Console.WriteLine("❌ All ports failed! Check for port conflicts.");
            Console.ReadKey();
        }

        private static async Task<bool> TryStartServer(int port)
        {
            try
            {
                var application = new ApplicationInstance
                {
                    ApplicationName = "Beverage Filling Line Server",
                    ApplicationType = ApplicationType.Server
                };

                var config = new ApplicationConfiguration
                {
                    ApplicationName = "Beverage Filling Line Server",
                    ApplicationUri = $"urn:localhost:BeverageFillingLineServer:{port}",
                    ApplicationType = ApplicationType.Server,

                    ServerConfiguration = new ServerConfiguration
                    {
                        BaseAddresses = new StringCollection { $"opc.tcp://localhost:{port}" },
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
                        MaxSessionTimeout = 60000
                    },

                    SecurityConfiguration = new SecurityConfiguration
                    {
                        AutoAcceptUntrustedCertificates = true,
                        RejectSHA1SignedCertificates = false,
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
                    }
                };

                application.ApplicationConfiguration = config;

                var server = new WorkingFinalServer();
                await application.Start(server);

                // If we get here, it worked!
                Console.WriteLine($"🎉 Server started successfully on port {port}!");
                Console.WriteLine("🔄 Simulation running...");

                // Keep it running
                while (true)
                {
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Port {port} failed: {ex.Message}");
                return false;
            }
        }
    }

    public class WorkingFinalServer : StandardServer
    {
        private BeverageFillingLineMachine m_machine;
        private Timer m_timer;

        public WorkingFinalServer()
        {
            m_machine = new BeverageFillingLineMachine();
        }

        protected override ServerProperties LoadServerProperties()
        {
            return new ServerProperties
            {
                ManufacturerName = "FluidFill Systems",
                ProductName = "Beverage Filling Line Server",
                SoftwareVersion = "1.0.0",
                BuildNumber = "1",
                BuildDate = DateTime.Now
            };
        }

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("🏗️  Creating working node manager...");

            try
            {
                var nodeManager = new WorkingFinalNodeManager(server, configuration, m_machine);
                var masterNodeManager = new MasterNodeManager(server, configuration, null, nodeManager);

                // Start simulation
                m_timer = new Timer(UpdateSimulation, null, 1000, 2000);

                Console.WriteLine("✅ Node manager and simulation started!");
                return masterNodeManager;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Node manager error: {ex.Message}");
                return new MasterNodeManager(server, configuration, null);
            }
        }

        private void UpdateSimulation(object state)
        {
            try
            {
                m_machine.UpdateSimulation();
                Console.WriteLine($"🔄 [{DateTime.Now:HH:mm:ss}] {m_machine.MachineStatus} | " +
                                $"Fill: {m_machine.ActualFillVolume:F1}ml | " +
                                $"Tank: {m_machine.ProductLevelTank:F1}% | " +
                                $"{m_machine.CurrentStation}");

                if (m_machine.ActiveAlarms.Count > 0)
                {
                    Console.WriteLine($"⚠️  ALARMS: {string.Join(" | ", m_machine.ActiveAlarms.Take(2))}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Simulation error: {ex.Message}");
            }
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

    public class WorkingFinalNodeManager : CustomNodeManager2
    {
        private BeverageFillingLineMachine m_machine;
        private Dictionary<string, BaseDataVariableState> m_variables;
        private Timer m_updateTimer;

        public WorkingFinalNodeManager(IServerInternal server, ApplicationConfiguration configuration, BeverageFillingLineMachine machine)
            : base(server, configuration, "urn:BeverageServer:")
        {
            m_machine = machine;
            m_variables = new Dictionary<string, BaseDataVariableState>();
            SetNamespaces("urn:BeverageServer:");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);
                m_updateTimer = new Timer(UpdateOpcVariables, null, 1500, 2000);
                Console.WriteLine("📋 OPC UA address space created with live variables");
            }
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();

            // Root folder
            FolderState root = new FolderState(null)
            {
                NodeId = new NodeId("BeverageFillingLine", NamespaceIndex),
                BrowseName = new QualifiedName("Beverage Filling Line", NamespaceIndex),
                DisplayName = new LocalizedText("Beverage Filling Line"),
                TypeDefinitionId = ObjectTypeIds.FolderType
            };

            root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            AddPredefinedNode(context, root);
            predefinedNodes.Add(root);

            // Key variables
            CreateVariable(root, "MachineStatus", DataTypeIds.String, m_machine.MachineStatus, predefinedNodes);
            CreateVariable(root, "ActualFillVolume", DataTypeIds.Double, m_machine.ActualFillVolume, predefinedNodes);
            CreateVariable(root, "TargetFillVolume", DataTypeIds.Double, m_machine.TargetFillVolume, predefinedNodes);
            CreateVariable(root, "ActualLineSpeed", DataTypeIds.Double, m_machine.ActualLineSpeed, predefinedNodes);
            CreateVariable(root, "ProductLevelTank", DataTypeIds.Double, m_machine.ProductLevelTank, predefinedNodes);
            CreateVariable(root, "CurrentStation", DataTypeIds.String, m_machine.CurrentStation, predefinedNodes);
            CreateVariable(root, "GoodBottles", DataTypeIds.UInt32, m_machine.GoodBottles, predefinedNodes);
            CreateVariable(root, "TotalBottles", DataTypeIds.UInt32, m_machine.TotalBottles, predefinedNodes);

            Console.WriteLine($"📊 Created {m_variables.Count} live OPC UA variables");
            return predefinedNodes;
        }

        private void CreateVariable(FolderState parent, string name, NodeId dataType, object initialValue, NodeStateCollection predefinedNodes)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent)
            {
                NodeId = new NodeId(name, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText(name),
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                DataType = dataType,
                ValueRank = ValueRanks.Scalar,
                AccessLevel = AccessLevels.CurrentRead,
                UserAccessLevel = AccessLevels.CurrentRead,
                Value = initialValue,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };

            parent.AddChild(variable);
            predefinedNodes.Add(variable);
            m_variables[name] = variable;
        }

        private void UpdateOpcVariables(object state)
        {
            try
            {
                lock (Lock)
                {
                    // Update OPC UA variables with current machine values
                    UpdateVariable("MachineStatus", m_machine.MachineStatus);
                    UpdateVariable("ActualFillVolume", m_machine.ActualFillVolume);
                    UpdateVariable("TargetFillVolume", m_machine.TargetFillVolume);
                    UpdateVariable("ActualLineSpeed", m_machine.ActualLineSpeed);
                    UpdateVariable("ProductLevelTank", m_machine.ProductLevelTank);
                    UpdateVariable("CurrentStation", m_machine.CurrentStation);
                    UpdateVariable("GoodBottles", m_machine.GoodBottles);
                    UpdateVariable("TotalBottles", m_machine.TotalBottles);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OPC UA update error: {ex.Message}");
            }
        }

        private void UpdateVariable(string name, object value)
        {
            if (m_variables.ContainsKey(name))
            {
                m_variables[name].Value = value;
                m_variables[name].Timestamp = DateTime.UtcNow;
                m_variables[name].ClearChangeMasks(SystemContext, false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
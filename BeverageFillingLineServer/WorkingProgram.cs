using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class WorkingProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Working Beverage Filling Line Server...");

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
                    ApplicationUri = "urn:localhost:BeverageFillingLineServer",
                    ApplicationType = ApplicationType.Server,

                    ServerConfiguration = new ServerConfiguration
                    {
                        BaseAddresses = new StringCollection { "opc.tcp://localhost:4840" },
                        SecurityPolicies = new ServerSecurityPolicyCollection
                        {
                            new ServerSecurityPolicy
                            {
                                SecurityMode = MessageSecurityMode.None,
                                SecurityPolicyUri = SecurityPolicies.None
                            }
                        }
                    },

                    SecurityConfiguration = new SecurityConfiguration
                    {
                        AutoAcceptUntrustedCertificates = true
                    }
                };

                application.ApplicationConfiguration = config;

                // Create working server
                var server = new WorkingServer();
                await application.Start(server);

                Console.WriteLine("Working server started at: opc.tcp://localhost:4840");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();

                server.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ReadKey();
            }
        }
    }

    public class WorkingServer : ServerBase
    {
        private Timer m_simulationTimer;
        private BeverageFillingLineMachine m_machine;
        private Dictionary<string, object> m_values;
        private WorkingNodeManager m_nodeManager;

        public WorkingServer()
        {
            m_machine = new BeverageFillingLineMachine();
            m_values = new Dictionary<string, object>();
            Console.WriteLine("WorkingServer created");
        }

        protected override void StartApplication(ApplicationConfiguration configuration)
        {
            Console.WriteLine("Starting WorkingServer application...");
            base.StartApplication(configuration);

            // Start simple simulation timer
            m_simulationTimer = new Timer(OnSimulationUpdate, null, 2000, 2000);
            Console.WriteLine("WorkingServer started with simulation (no OPC UA nodes yet)");
        }

        private void OnSimulationUpdate(object state)
        {
            try
            {
                m_machine.UpdateSimulation();

                // Update values dictionary
                m_values["MachineStatus"] = m_machine.MachineStatus;
                m_values["ActualFillVolume"] = m_machine.ActualFillVolume;
                m_values["ProductLevelTank"] = m_machine.ProductLevelTank;
                m_values["CurrentStation"] = m_machine.CurrentStation;

                // Print some values to show it's working
                Console.WriteLine($"Status: {m_machine.MachineStatus}, Fill: {m_machine.ActualFillVolume:F1}ml, Tank: {m_machine.ProductLevelTank:F1}%, Station: {m_machine.CurrentStation}");

                if (m_machine.ActiveAlarms.Count > 0)
                {
                    Console.WriteLine($"ALARMS: {string.Join(", ", m_machine.ActiveAlarms)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Simulation error: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_simulationTimer?.Dispose();
                m_nodeManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class WorkingNodeManager : CustomNodeManager2
    {
        private BeverageFillingLineMachine m_machine;
        private Dictionary<string, BaseDataVariableState> m_variables;
        private Timer m_updateTimer;

        public WorkingNodeManager(IServerInternal server, ApplicationConfiguration configuration, BeverageFillingLineMachine machine)
            : base(server, configuration, "http://fluidfill.com/working/")
        {
            m_machine = machine;
            m_variables = new Dictionary<string, BaseDataVariableState>();
            SetNamespaces("http://fluidfill.com/working/");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);

                // Start variable update timer
                m_updateTimer = new Timer(UpdateVariables, null, 1000, 1000);
                Console.WriteLine("OPC UA address space created with update timer");
            }
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();

            // Create root folder
            FolderState root = new FolderState(null)
            {
                NodeId = new NodeId("BeverageFillingLine", NamespaceIndex),
                BrowseName = new QualifiedName("Beverage Filling Line", NamespaceIndex),
                DisplayName = new LocalizedText("en", "Beverage Filling Line"),
                TypeDefinitionId = ObjectTypeIds.FolderType
            };

            root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            AddPredefinedNode(context, root);
            predefinedNodes.Add(root);

            // Create some key variables
            CreateVariable(root, "MachineStatus", "Machine Status", DataTypeIds.String, m_machine.MachineStatus, predefinedNodes);
            CreateVariable(root, "ActualFillVolume", "Actual Fill Volume", DataTypeIds.Double, m_machine.ActualFillVolume, predefinedNodes);
            CreateVariable(root, "TargetFillVolume", "Target Fill Volume", DataTypeIds.Double, m_machine.TargetFillVolume, predefinedNodes);
            CreateVariable(root, "ActualLineSpeed", "Actual Line Speed", DataTypeIds.Double, m_machine.ActualLineSpeed, predefinedNodes);
            CreateVariable(root, "ProductLevelTank", "Product Level Tank", DataTypeIds.Double, m_machine.ProductLevelTank, predefinedNodes);
            CreateVariable(root, "CurrentStation", "Current Station", DataTypeIds.String, m_machine.CurrentStation, predefinedNodes);
            CreateVariable(root, "GoodBottles", "Good Bottles", DataTypeIds.UInt32, m_machine.GoodBottles, predefinedNodes);
            CreateVariable(root, "TotalBottles", "Total Bottles", DataTypeIds.UInt32, m_machine.TotalBottles, predefinedNodes);

            Console.WriteLine($"Created {m_variables.Count} OPC UA variables");
            return predefinedNodes;
        }

        private void CreateVariable(FolderState parent, string name, string displayName, NodeId dataType, object initialValue, NodeStateCollection predefinedNodes)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent)
            {
                NodeId = new NodeId(name, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", displayName),
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

        private void UpdateVariables(object state)
        {
            try
            {
                lock (Lock)
                {
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
                Console.WriteLine($"Error updating OPC UA variables: {ex.Message}");
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
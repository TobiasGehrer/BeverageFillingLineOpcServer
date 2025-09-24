using Opc.Ua;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class BeverageFillingLineNodeManager : CustomNodeManager2
    {
        private Timer m_simulationTimer;
        private BeverageFillingLineMachine m_machine;
        private Dictionary<string, BaseDataVariableState> m_variables;

        public BeverageFillingLineNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration, "http://fluidfill.com/machine/")
        {
            m_variables = new Dictionary<string, BaseDataVariableState>();
            SetNamespaces("http://fluidfill.com/machine/");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);

                // Start simulation timer
                m_simulationTimer = new Timer(OnSimulationTimer, null, 1000, 1000);
            }
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();
            m_machine = new BeverageFillingLineMachine();

            // Create a simple root folder for testing
            FolderState root = new FolderState(null)
            {
                NodeId = new NodeId("FluidFillExpress2", NamespaceIndex),
                BrowseName = new QualifiedName("FluidFill Express #2", NamespaceIndex),
                DisplayName = new LocalizedText("en", "FluidFill Express #2"),
                TypeDefinitionId = ObjectTypeIds.FolderType
            };
            
            root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            AddPredefinedNode(context, root);
            predefinedNodes.Add(root);
            
            return predefinedNodes;
        }

        private void CreateMethods(FolderState parent)
        {
            MethodState startMethod = new MethodState(parent)
            {
                NodeId = new NodeId("StartMachine", NamespaceIndex),
                BrowseName = new QualifiedName("StartMachine", NamespaceIndex),
                DisplayName = new LocalizedText("en", "Start Machine"),
                UserExecutable = true,
                Executable = true
            };
            startMethod.OnCallMethod = OnStartMachine;
            parent.AddChild(startMethod);

            MethodState stopMethod = new MethodState(parent)
            {
                NodeId = new NodeId("StopMachine", NamespaceIndex),
                BrowseName = new QualifiedName("StopMachine", NamespaceIndex),
                DisplayName = new LocalizedText("en", "Stop Machine"),
                UserExecutable = true,
                Executable = true
            };
            stopMethod.OnCallMethod = OnStopMachine;
            parent.AddChild(stopMethod);
        }

        private ServiceResult OnStartMachine(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_machine.StartMachine();
            Console.WriteLine(">> Start Machine command executed");
            return ServiceResult.Good;
        }

        private ServiceResult OnStopMachine(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_machine.StopMachine();
            Console.WriteLine(">> Stop Machine command executed");
            return ServiceResult.Good;
        }

        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            FolderState folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            if (parent != null)
            {
                parent.AddChild(folder);
            }

            return folder;
        }

        private BaseDataVariableState CreateVariable(NodeState parent, string path, string name, NodeId dataType, int valueRank, object initialValue)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                DataType = dataType,
                ValueRank = valueRank,
                AccessLevel = AccessLevels.CurrentRead,
                UserAccessLevel = AccessLevels.CurrentRead,
                Historizing = false,
                Value = initialValue,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            m_variables[path] = variable;
            return variable;
        }

        private void OnSimulationTimer(object state)
        {
            try
            {
                lock (Lock)
                {
                    m_machine.UpdateSimulation();
                    UpdateVariables();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Simulation Error: {ex.Message}");
            }
        }

        private void UpdateVariables()
        {
            UpdateVariable("ActualFillVolume", m_machine.ActualFillVolume);
            UpdateVariable("FillDeviation", m_machine.FillDeviation);
            UpdateVariable("ActualLineSpeed", m_machine.ActualLineSpeed);
            UpdateVariable("ActualProductTemperature", m_machine.ActualProductTemperature);
            UpdateVariable("MachineStatus", m_machine.MachineStatus);
            UpdateVariable("CurrentStation", m_machine.CurrentStation);
            UpdateVariable("ProductLevelTank", m_machine.ProductLevelTank);
            UpdateVariable("GoodBottles", m_machine.GoodBottles);
            UpdateVariable("TotalBottles", m_machine.TotalBottles);
        }

        private void UpdateVariable(string path, object value)
        {
            if (m_variables.ContainsKey(path))
            {
                m_variables[path].Value = value;
                m_variables[path].Timestamp = DateTime.UtcNow;
                m_variables[path].ClearChangeMasks(SystemContext, false);
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
}

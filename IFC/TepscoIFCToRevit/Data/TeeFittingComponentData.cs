using Autodesk.Revit.DB;
using System.Collections.Generic;
using TepscoIFCToRevit.Data.MEPData;

namespace TepscoIFCToRevit.Data
{
    public class TeeFittingComponentData
    {
        #region Variable

        private List<PipeData> m_PipeToFitting = new List<PipeData>();
        private List<Connector> m_lstConnector = new List<Connector>();
        private List<DuctData> m_DuctToFitting = new List<DuctData>();

        #endregion Variable

        #region Property

        //PipeData use to create fitting and will be detele (use when create pipe tee fitting)
        public List<PipeData> PipeToFitting { get => m_PipeToFitting; set => m_PipeToFitting = value; }

        //DuctData use to create fitting and will be detele
        public List<DuctData> DuctToFitting { get => m_DuctToFitting; set => m_DuctToFitting = value; }

        //Tee fitting connectors
        public List<Connector> Connectors { get => m_lstConnector; set => m_lstConnector = value; }

        #endregion Property

        #region Contructor

        public TeeFittingComponentData(PipeData pipeData1, PipeData pipeData2, List<Connector> connectors)
        {
            m_PipeToFitting.Add(pipeData1);
            m_PipeToFitting.Add(pipeData2);
            m_lstConnector.AddRange(connectors);
        }

        public TeeFittingComponentData(DuctData ductData1, DuctData ductData2, List<Connector> connectors)
        {
            m_DuctToFitting.Add(ductData1);
            m_DuctToFitting.Add(ductData2);
            m_lstConnector.AddRange(connectors);
        }

        #endregion Contructor
    }
}
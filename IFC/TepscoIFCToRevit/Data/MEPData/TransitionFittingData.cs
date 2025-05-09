using Autodesk.Revit.DB;
using System.Collections.Generic;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.MEPData
{
    public class TransitionFittingData
    {
        #region Variable & Properties

        private Document _doc = null;

        // New Fitting Id
        public ElementId NewCreateFittingId = ElementId.InvalidElementId;

        public FamilyInstance NewCreateFitting = null;

        // Connector 1
        private Connector _mainConnector1 = null;

        // Connector 2
        private Connector _mainConnector2 = null;

        public bool IsValidate { get => NewCreateFitting != null && NewCreateFitting.IsValidObject; }

        #endregion Variable & Properties

        #region Constructor

        public TransitionFittingData(Document document, List<Connector> lstConnector)
        {
            _doc = document;
            if (lstConnector.Count == 2)
            {
                _mainConnector1 = lstConnector[0];
                _mainConnector2 = lstConnector[1];
            }
        }

        #endregion Constructor

        #region Method

        public bool IsCreateTransitionFitting()
        {
            if (_doc != null && _mainConnector1 != null && _mainConnector1.IsValidObject
                && _mainConnector2 != null && _mainConnector2.IsValidObject
                && !_mainConnector1.IsConnected
                && !_mainConnector2.IsConnected
                && !_mainConnector1.IsConnectedTo(_mainConnector2))
            {
                if (_mainConnector1.Shape == _mainConnector2.Shape)
                {
                    ConnectorProfileType shape = _mainConnector1.Shape;

                    if (shape == ConnectorProfileType.Round)
                    {
                        if (_mainConnector1.Radius == _mainConnector2.Radius)
                            return false;
                    }
                    else if (shape == ConnectorProfileType.Oval || shape == ConnectorProfileType.Rectangular)
                    {
                        if (_mainConnector1.Height == _mainConnector2.Height && _mainConnector1.Width == _mainConnector2.Width)
                            return false;
                    }

                    return true;
                }
            }

            return false;
        }

        public void CreateTransactionFitting()
        {
            if (_mainConnector1.IsValidObject && _mainConnector2.IsValidObject)
            {
                XYZ origin = _mainConnector1.Origin;

                NewCreateFitting = _doc.Create.NewTransitionFitting(_mainConnector1, _mainConnector2);

                App._UIDoc.Document.Regenerate();

                if (_mainConnector1.IsValidObject) // after regenerate has some pipe has been deleted
                {
                    MEPCurve mEPCurve1 = _mainConnector1?.Owner as MEPCurve;

                    if (mEPCurve1 != null && NewCreateFitting != null)
                    {
                        Connector con1 = RevitUtils.GetConnectorNearest(origin, NewCreateFitting?.MEPModel?.ConnectorManager, out Connector con2);

                        XYZ transition = (con1.Origin - origin);

                        ElementTransformUtils.MoveElement(App._UIDoc.Document, NewCreateFitting.Id, transition);
                    }
                }
            }
        }

        #endregion Method
    }
}
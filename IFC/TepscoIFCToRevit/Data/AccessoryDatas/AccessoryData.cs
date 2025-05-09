using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.AccessoryDatas
{
    public class AccessoryData : ElementConvert
    {
        private XYZ position;
        private XYZ center;
        private Line location;
        private List<Element> mepConnects = new List<Element>();

        public AccessoryData(UIDocument uiDoc,
                             LinkElementData linkElementData,
                             ElementId typeId,
                             RevitLinkInstance revLinkIns,
                             ConvertParamData paramData)
        {
            if (uiDoc != null
               && linkElementData != null
               && linkElementData.LinkElement != null
               && linkElementData.LinkElement.IsValidObject
               && revLinkIns != null)
            {
                _uiDoc = uiDoc;
                _doc = _uiDoc.Document;
                LinkEleData = linkElementData;
                TypeId = typeId;
                LinkInstance = revLinkIns;
                ParameterData = paramData;
            }
        }

        public void Initialize()
        {
            var elemFilters = new FilteredElementCollector(_doc).WhereElementIsNotElementType().OfClass(typeof(MEPCurve)).ToElementIds();

            BoundingBoxXYZ box = GeometryUtils.GetBoudingBoxExtend(LinkEleData.LinkElement, LinkTransform);
            center = (box.Max + box.Min) / 2;

            mepConnects = GeometryUtils.FindPipeDuctNearestBox(_doc, new List<ElementId>(elemFilters), box);
            if (mepConnects?.Count == 1 || mepConnects?.Count == 2)
            {
                if (mepConnects[0] is MEPCurve mep && mep.Location is LocationCurve lcCurve && lcCurve.Curve is Line lcLine)
                {
                    location = Line.CreateUnbound(lcLine.Origin, lcLine.Direction);
                    position = location.Project(center).XYZPoint;

                    if (_doc.GetElement(TypeId) is FamilySymbol symbol)
                    {
                        if (!symbol.IsActive)
                        {
                            symbol.Activate();
                        }

                        ConvertElem = _doc.Create.NewFamilyInstance(position, symbol, mep.ReferenceLevel, StructuralType.NonStructural);
                    }

                    if (ConvertElem?.IsValidObject != true)
                    {
                        return;
                    }

                    RevitUtils.SetValueParamterConvert(_uiDoc, ConvertElem, LinkEleData, ParameterData);
                }
            }
        }

        public void Roatate()
        {
            if (ConvertElem.Location is LocationPoint lcPoint)
            {
                XYZ move = position - lcPoint.Point;
                if (!move.IsZeroLength())
                {
                    ElementTransformUtils.MoveElement(_doc, ConvertElem.Id, move);
                }
            }

            double length = RevitUtils.MIN_LENGTH;
            var connectorsSet = (ConvertElem as FamilyInstance).MEPModel?.ConnectorManager?.Connectors;
            if (connectorsSet?.Size == 2)
            {
                List<Connector> connectors = new List<Connector>();
                foreach (Connector connector in connectorsSet)
                {
                    connectors.Add(connector);
                }

                XYZ direction = connectors.First().Origin - connectors.Last().Origin;
                length = direction.GetLength();
                if (!RevitUtils.IsParallel(location.Direction, direction))
                {
                    double angle = location.Direction.AngleTo(direction);
                    Line axis = Line.CreateUnbound(position, location.Direction.CrossProduct(direction));

                    ElementTransformUtils.RotateElement(_doc, ConvertElem.Id, axis, angle);
                }
            }

            XYZ originDirection = center - position;
            if (!originDirection.IsZeroLength())
            {
                var box = ConvertElem.get_BoundingBox(null);
                center = (box.Max + box.Min) / 2;

                XYZ currentDirection = center - position;
                double angle = currentDirection.AngleOnPlaneTo(originDirection, location.Direction);

                if (angle > 0)
                {
                    ElementTransformUtils.RotateElement(_doc, ConvertElem.Id, location, angle);
                }
            }

            if (connectorsSet?.Size == 2)
            {
                foreach (Connector connector in connectorsSet)
                {
                    Connector pipeConnet = null;
                    double distance = 2 * length;
                    bool isFind = false;
                    foreach (var item in mepConnects)
                    {
                        if (item is MEPCurve mepItem)
                        {
                            var set = mepItem.ConnectorManager?.Connectors;
                            if (!(set?.Size > 0))
                            {
                                continue;
                            }

                            foreach (Connector cn in set)
                            {
                                if (cn.IsConnected)
                                {
                                    continue;
                                }

                                XYZ vector = cn.Origin - connector.Origin;
                                double d = vector.GetLength();
                                if (d < RevitUtilities.Common.MIN_LENGTH)
                                {
                                    pipeConnet = cn;
                                    isFind = true;
                                    break;
                                }

                                if (d < distance)
                                {
                                    distance = d;
                                    pipeConnet = cn;
                                }
                            }
                        }

                        if (isFind)
                        {
                            break;
                        }
                    }

                    if (pipeConnet != null)
                    {
                        if (isFind)
                        {
                            connector.ConnectTo(pipeConnet);
                        }
                        //else if (RevitUtilities.Common.IsBetween(pipeConnet.Origin, position, connector.Origin))
                        //{
                        //    connector.ConnectTo(pipeConnet);
                        //}
                    }
                }
            }
        }
    }
}
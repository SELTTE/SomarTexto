using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Seltte.AutoCAD.AcadToolkit.License;
using System;
using System.Collections.Generic;

namespace SomarTexto
{
    public class Class1
    {
        [CommandMethod("st", CommandFlags.Modal)]
        public void SomarTextos()
        {
            if (!EnsureLicense())
                return;

            Editor ed = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            TypedValue[] tvs = new[]
            {
                new TypedValue(0, "TEXT"), // only DBText or MText
            };

            SelectionFilter filter = new SelectionFilter(tvs);
            PromptSelectionResult selection = ed.GetSelection(filter);

            if (selection.Status != PromptStatus.OK)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<double> lista = new List<double>();

                foreach (SelectedObject obj in selection.Value)
                {
                    DBText text = (DBText)tr.GetObject(obj.ObjectId, OpenMode.ForRead);

                    string textModified = text.TextString;
                    textModified = textModified.Split('/')[0]; // Apenas por conveniência, a parte esquerda do
                                                               // número será usado para a soma

                    if (textModified.ToString().Contains("+"))
                    {
                        lista.Add(Convert.ToDouble(textModified.ToString().Split('+')[0]));
                        lista.Add(Convert.ToDouble(textModified.ToString().Split('+')[1].Replace("*", "").Replace("(2)", "").Trim()));
                    }
                    else if (textModified.ToString().Contains(" "))
                    {
                        string[] texts = textModified.Split(' ');

                        foreach (var item in texts)
                        {
                            if (!(item == "" || item == " " || item == "  "))
                            {
                                lista.Add(Convert.ToDouble(item.Replace("*", "").Replace("(2)", "").Trim()));
                            }
                        }
                    }
                    else
                    {
                        lista.Add(Convert.ToDouble(textModified));
                    }
                }

                double soma = 0;
                foreach (var item in lista)
                {
                    soma += item;
                }

                // Start a transaction
                using (Transaction acTrans = db.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable acBlkTbl;
                    acBlkTbl = acTrans.GetObject(db.BlockTableId,
                                                    OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord acBlkTblRec;
                    acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                    OpenMode.ForWrite) as BlockTableRecord;

                    // Create a single-line text object
                    using (DBText acText = new DBText())
                    {
                        PromptPointResult pPtRes;
                        PromptPointOptions pPtOpts = new PromptPointOptions("");

                        // Prompt for the start point
                        pPtOpts.Message = "\nPonto de inserção do somatório: ";
                        pPtRes = doc.Editor.GetPoint(pPtOpts);
                        Point3d ptStart = pPtRes.Value;

                        if (soma != 0)
                        {
                            acText.Position = ptStart;
                            acText.Height = 35;
                            acText.TextString = soma.ToString();
                        }
                        else
                        {
                            ed.WriteMessage("\nNada a somar!");
                        }

                        acBlkTblRec.AppendEntity(acText);
                        acTrans.AddNewlyCreatedDBObject(acText, true);
                    }

                    // Save the changes and dispose of the transaction
                    acTrans.Commit();
                }

                tr.Commit();
            }
        }

        private static bool EnsureLicense()
        {
            try
            {
                LicenseGuard.Require();
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}

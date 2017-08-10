using System;
using System.Collections.Generic;

#region Autodesk
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
#endregion

namespace _AcSpeedy_Block_Attr
{
    public static class Block
    {
        internal static void AddingAttributeToBlock(string blkName)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);

                if (!bt.Has(blkName))
                {
                    ed.WriteMessage("\nBlock definition PART does not  exist");
                    return;
                }

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blkName], OpenMode.ForRead, false);

                // location of the AttributeDefinition in the 
                // block definition 
                Point3d ptloc = new Point3d(0, 2, 0);

                // create a AttributeDefinition
                // specify the text,tag and prompt
                string attrValue = "NEW VALUE ADDED";
                string attrTag = "MYTAG";
                string strprompt = "Enter a new value";

                // used current text style
                AttributeDefinition attDef = new AttributeDefinition(ptloc, attrValue, attrTag, strprompt, db.Textstyle);
                attDef.Height = 0.12;
                attDef.Layer = "0";
                attDef.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 0);
                attDef.LinetypeId = db.ContinuousLinetype;

                // append the AttributeDefinition to the definition 
                btr.UpgradeOpen();
                btr.AppendEntity(attDef);
                tr.AddNewlyCreatedDBObject(attDef, true);
                btr.DowngradeOpen();
                tr.Commit();
            }
        }
    }
}

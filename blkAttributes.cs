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
        static RXClass attDefClass = RXClass.GetClass(typeof(AttributeDefinition));

        private static List<AttributeDefinition> GetAttributes(this BlockTableRecord target, Transaction tr)
        {
            List<AttributeDefinition> attDefs = new List<AttributeDefinition>();
            foreach (ObjectId id in target)
            {
                if (id.ObjectClass == attDefClass)
                {
                    AttributeDefinition attDef = (AttributeDefinition)tr.GetObject(id, OpenMode.ForRead);
                    attDefs.Add(attDef);
                }
            }
            return attDefs;
        }

        private static void ResetAttributes(this BlockReference br, List<AttributeDefinition> attDefs, Transaction tr)
        {
            Dictionary<string, string> attValues = new Dictionary<string, string>();
            foreach (ObjectId id in br.AttributeCollection)
            {
                if (!id.IsErased)
                {
                    AttributeReference attRef = (AttributeReference)tr.GetObject(id, OpenMode.ForWrite);
                    attValues.Add(attRef.Tag,
                        attRef.IsMTextAttribute ? attRef.MTextAttribute.Contents : attRef.TextString);
                    attRef.Erase();
                }
            }
            foreach (AttributeDefinition attDef in attDefs)
            {
                AttributeReference attRef = new AttributeReference();
                attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                if (attDef.Constant)
                {
                    attRef.TextString = attDef.IsMTextAttributeDefinition ?
                        attDef.MTextAttributeDefinition.Contents :
                        attDef.TextString;
                }
                else if (attValues.ContainsKey(attRef.Tag))
                {
                    attRef.TextString = attValues[attRef.Tag];
                }
                br.AttributeCollection.AppendAttribute(attRef);
                tr.AddNewlyCreatedDBObject(attRef, true);
            }
        }

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

        public static void SynchronizeAttributes(this BlockTableRecord target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            Transaction tr = target.Database.TransactionManager.TopTransaction;
            if (tr == null)
                throw new Autodesk.AutoCAD.Runtime.Exception(ErrorStatus.NoActiveTransactions);
            List<AttributeDefinition> attDefs = target.GetAttributes(tr);
            foreach (ObjectId id in target.GetBlockReferenceIds(true, false))
            {
                BlockReference br = (BlockReference)tr.GetObject(id, OpenMode.ForWrite);
                br.ResetAttributes(attDefs, tr);
            }
            if (target.IsDynamicBlock)
            {
                target.UpdateAnonymousBlocks();
                foreach (ObjectId id in target.GetAnonymousBlockIds())
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    attDefs = btr.GetAttributes(tr);
                    foreach (ObjectId brId in btr.GetBlockReferenceIds(true, false))
                    {
                        BlockReference br = (BlockReference)tr.GetObject(brId, OpenMode.ForWrite);
                        br.ResetAttributes(attDefs, tr);
                    }
                }
            }
        }

        public static void FillAttribute(string blkName, string attrTag, string attrValue)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blkName)) return; // Replace "parent" by the parent block name
                BlockTableRecord btr =
                    (BlockTableRecord)tr.GetObject(bt[blkName], OpenMode.ForRead);
                foreach (ObjectId id in btr)
                {
                    BlockReference br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (br != null && br.Name == blkName) // replace "nested" withe the nested block name
                    {
                        foreach (ObjectId attId in br.AttributeCollection)
                        {
                            AttributeReference att =
                                (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                            if (att.Tag == attrTag) // replace "TAG" with the attribute tag
                            {
                                att.UpgradeOpen();
                                att.TextString = attrValue; // repace "foo" with the new attribute value
                                break;
                            }
                        }
                        break;
                    }
                }
                tr.Commit();
            }
            ed.Regen();
        }
    }
}

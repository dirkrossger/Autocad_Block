using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

#region Autodesk
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
#endregion

using _AcSpeedy;

namespace _AcSpeedy_Block_Get
{
    class Block
    {
        public string Attr { get; set; }
        public Point3d Position { get; set; }
        public string AttrTag { get; set; }


        private Database db;
        private Transaction tr;

        public Block()
        {
        }

        public Block(string attr, string attrTag, Point3d position)
        {
            this.Attr = attr;
            this.Position = position;
            this.AttrTag = attrTag;
        }


        public IEnumerable<BlockTableRecord> Blocks
        {
            // https://wtertinek.com/2015/05/31/linq-and-the-autocad-net-api-part-4/
            get { return GetTableItems<BlockTableRecord>(db.BlockTableId); }
        }

        private IEnumerable<T> GetTableItems<T>(ObjectId tableID) where T : SymbolTableRecord
        {
            if (tableID.IsValid)
            {
                var table = (IEnumerable)tr.GetObject(tableID, OpenMode.ForRead);

                foreach (ObjectId id in table)
                {
                    yield return (T)tr.GetObject(id, OpenMode.ForRead);
                }
            }
            else
            {
                yield break;
            }
        }

        public static bool GetBlock(Database db, string blockName)
        {
            bool result = false;
            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                    // Test each entity in the container...

                    foreach (ObjectId entId in btr)
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;

                        if (ent != null)
                        {
                            BlockReference br = ent as BlockReference;
                            if (br != null)
                            {
                                BlockTableRecord bd = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);

                                // ... to see whether it's a block with
                                // the name we're after

                                if (bd.Name == blockName)
                                {
                                    result = true;
                                    return result;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        private static List<Block> GetAttributesInBlock(Transaction tr, BlockReference br)
        {
            // Will return the number of attributes modified

            List<Block> block = new List<Block>();
            Dictionary<string, string> attribute = new Dictionary<string, string>();

            try
            {
                if (br != null)
                {
                    BlockTableRecord bd = (BlockTableRecord)Active.Database.TransactionManager.GetObject(br.BlockTableRecord, OpenMode.ForRead);

                    // ... to see whether it's a block with
                    // the name we're after

                    //if (bd.Name == blockName)
                    if (br.AttributeCollection.Count > 0)//(br.AttributeCollection.Count > 0 && bd.Name != "xreflist")
                    {
                        attribute = new Dictionary<string, string>();
                        // Check each of the attributes...

                        foreach (ObjectId arId in br.AttributeCollection)
                        {
                            DBObject obj = tr.GetObject(arId, OpenMode.ForRead);
                            AttributeReference ar = obj as AttributeReference;
                            
                            if (ar != null)
                            {
                                //attribute.Add(ar.Tag, ar.TextString);
                                block.Add(new Block { Position = br.Position, Attr = ar.TextString, AttrTag = ar.Tag });
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex) { }

            return block;
        }

        public static List<Block> SelectBlock(string message)
        {
            PromptEntityOptions options = new PromptEntityOptions(message);
            options.SetRejectMessage("\nThe selected object is not a Block.\n");
            options.AddAllowedClass(typeof(BlockReference), false);
            PromptEntityResult peo = Active.Editor.GetEntity(options);

            List<Block> result = null;

            using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
            {
                switch (peo.Status)
                {
                    case PromptStatus.OK:
                        BlockReference br = tr.GetObject(peo.ObjectId, OpenMode.ForRead) as BlockReference;
                        result = Block.GetAttributesInBlock(tr, br);
                        break;
                    case PromptStatus.Cancel:
                        Active.Editor.WriteMessage("Select canceled");
                        return null;
                }
                tr.Commit();

                return result;
            }
        }

        public void GetSelectedBlock()
        {
            List<Block> result = null;
            result = SelectBlock("Select a Block!");

            Active.Editor.WriteMessage("\nAttribute:");

            foreach (Block b in result)
            {
                Active.Editor.WriteMessage("\nTag=" + b.AttrTag + " Value=" + b.Attr);
            }
        }

        public static void ApplyAttibutes(Database db, Transaction tr, BlockReference bref, string attrValue)
        {
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bref.BlockTableRecord, OpenMode.ForRead);
            int i = 0;

            foreach (ObjectId attId in btr)
            {
                Entity ent = (Entity)tr.GetObject(attId, OpenMode.ForRead);
                if (ent is AttributeDefinition)
                {
                    AttributeDefinition attDef = (AttributeDefinition)ent;
                    AttributeReference attRef = new AttributeReference();

                    attRef.SetAttributeFromBlock(attDef, bref.BlockTransform);
                    bref.AttributeCollection.AppendAttribute(attRef);
                    tr.AddNewlyCreatedDBObject(attRef, true);
                    attRef.TextString = attrValue.ToString();
                    attRef.AdjustAlignment(db);
                }
            }
        }
    }
}

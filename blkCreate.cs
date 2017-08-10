using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#region Autodesk
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
#endregion

using _AcSpeedy;
using _AcSpeedy_Block_Attr;

namespace _AcSpeedy_Block_Create
{
    public class Block
    {
        public void DefineBlock(string blkName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                // Get the block table from the drawing
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                try
                {
                    if (bt.Has(blkName))
                        ed.WriteMessage("\nA block with this name already exists.");
                    else
                    {
                        // Create our new block table record...
                        BlockTableRecord btr = new BlockTableRecord();

                        // ... and set its properties
                        btr.Name = blkName;

                        // Add the new block to the block table
                        bt.UpgradeOpen();
                        ObjectId btrId = bt.Add(btr);
                        tr.AddNewlyCreatedDBObject(btr, true);

                        // Add some lines to the block to form a square
                        // (the entities belong directly to the block)
                        DBObjectCollection ents = SquareOfLines(1);
                        foreach (Entity ent in ents)
                        {
                            btr.AppendEntity(ent);
                            tr.AddNewlyCreatedDBObject(ent, true);
                        }

                        // Add a block reference to the model space
                        //BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                        //BlockReference br = new BlockReference(Point3d.Origin, btrId);
                        //ms.AppendEntity(br);
                        //tr.AddNewlyCreatedDBObject(br, true);

                        _AcSpeedy_Block_Attr.Block.AddingAttributeToBlock(blkName);

                        // Commit the transaction
                        tr.Commit();

                        // Report what we've done
                        //ed.WriteMessage("\nCreated block named \"{0}\" containing {1} entities.", blkName, ents.Count);
                    }
                }
                catch
                {
                    // An exception has been thrown, indicating the
                    // name is invalid
                    ed.WriteMessage("\nInvalid block name.");
                }


            }
        }

        public void InsertBlock(Point3dCollection Point3dColl, string blkName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                // Get the block table from the drawing
                BlockTable acBlkTbl = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                try
                {
                    if (!acBlkTbl.Has(blkName))
                        DefineBlock(blkName);

                    ObjectId blkRecId = ObjectId.Null;
                    blkRecId = acBlkTbl[blkName];

                    if (blkRecId != ObjectId.Null)
                    {
                        foreach (Point3d pt in Point3dColl)
                        {
                            using (BlockReference acBlkRef = new BlockReference(pt, blkRecId))
                            {
                                BlockTableRecord acCurSpaceBlkTblRec;
                                acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                                acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                tr.AddNewlyCreatedDBObject(acBlkRef, true);
                            }
                        }
                    }
                }
                catch
                {
                    // An exception has been thrown, indicating the
                    // name is invalid
                    ed.WriteMessage("\nInvalid block name.");
                }

                // Commit the transaction
                tr.Commit();
            }
        }

        private DBObjectCollection SquareOfLines(double size)
        {
            // A function to generate a set of entities for our block

            DBObjectCollection ents = new DBObjectCollection();
            Point3d[] pts =
                { new Point3d(-size, -size, 0),
            new Point3d(size, -size, 0),
            new Point3d(size, size, 0),
            new Point3d(-size, size, 0)
          };
            int max = pts.GetUpperBound(0);

            for (int i = 0; i <= max; i++)
            {
                int j = (i == max ? 0 : i + 1);
                Line ln = new Line(pts[i], pts[j]);
                ents.Add(ln);
            }
            return ents;
        }

    }
}
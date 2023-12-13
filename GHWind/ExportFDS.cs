using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Drawing;
using GH_IO.Serialization;
using System.Net.NetworkInformation;
using Rhino.Geometry.Collections;

/*
 * GHExportGeometry.cs
 * Copyright 2017 Christoph Waibel <chwaibel@student.ethz.ch>
 * 
 * This work is licensed under the GNU GPL license version 3.
*/

namespace GHWind
{
    public class GHExportFDS : GH_Component
    {

        bool export;

        public GHExportFDS()
            : base("Export FDS", "Export FDS",
                "Export geometry into a *.fds file",
                "EnergyHubs", "Wind Simulation")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //0
            pManager.AddBooleanParameter("export?", "export?", "export data? use a button", GH_ParamAccess.item);
            //1
            pManager.AddTextParameter("path", "path", "path to export geometry data to", GH_ParamAccess.item);
            //2
            pManager.AddPointParameter("origin", "origin", "origin", GH_ParamAccess.item);
            //3
            pManager.AddBrepParameter("bounding box", "box", "bounding box as brep", GH_ParamAccess.item);
            //4
            pManager.AddGenericParameter("cubes as doubles", "cubes", "cubes as list of double[xmin, xmax, ymin, ymax, zmin, zmax] (Discr_Mesh output)", GH_ParamAccess.list);
            pManager[4].Optional = true;
            //5
            pManager.AddSurfaceParameter("Inlet Surfaces", "Inlet Surfaces", "List of inlet surfaces (rectangular)", GH_ParamAccess.list);
            //6
            pManager.AddSurfaceParameter("Outlet Surfaces", "Outlet Surfaces", "List of outlet surfaces (rectangular)", GH_ParamAccess.list);
            //7
            pManager.AddTextParameter("&HEAD text", "&HEAD text", "connect panel that contains the full command of the &HEAD line in FDS format", GH_ParamAccess.item);
            //8
            pManager.AddNumberParameter("End Time", "End Time", "End time as single number", GH_ParamAccess.item);
            //9
            pManager.AddIntegerParameter("Number of Frames", "NFrames", "Number of steps to produce output files for.", GH_ParamAccess.item);
            //10
            pManager.AddTextParameter("IJK", "I J K", "Number of cells in the X, Y, and Z directions as a list.", GH_ParamAccess.list);
            //11
            pManager.AddNumberParameter("Supply Flow", "Supply Flow", "Supply flow in m3/s", GH_ParamAccess.item);
            //12
            pManager.AddNumberParameter("Exhaust Flow", "Exhaust Flow", "Exhaust flow in m3/s", GH_ParamAccess.item);
            //13
            pManager.AddTextParameter("Slices", "Slices", "Slices", GH_ParamAccess.list);
            //14
            pManager.AddIntegerParameter("Vent Type", "Vent Type", "Coefficients for Vent Type. 0 = Linear; 1 = Louvered; 2 = Radial; 3 = Spiral.", GH_ParamAccess.item);



        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
          
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            DA.GetData(0, ref export);

            string path = null;
            if (!DA.GetData(1, ref path)) { return; };

            Point3d origin = Point3d.Unset;
            if (!DA.GetData(2, ref origin)) { return; }

            Brep mesh = new Brep();
            if (!DA.GetData(3, ref mesh)) { return; }

            List<double[]> obsts = new List<double[]>();
            DA.GetDataList(4, obsts);

            List<Surface> inlets = new List<Surface>();
            if (!DA.GetDataList(5, inlets)) { return; };

            List<Surface> outlets = new List<Surface>();
            if (!DA.GetDataList(6, outlets)) { return; }

            string head = null;
            if (!DA.GetData(7, ref head)) { return; };

            double time = 60.0;
            if (!DA.GetData(8, ref time)) { return; };

            int nframes = 1000;
            if (!DA.GetData(9, ref nframes)) { return; };

            List<string> ijk = new List<string>();
            if (!DA.GetDataList(10, ijk)) { return; };

            double supply = 1.0;
            if (!DA.GetData(11, ref supply)) { return; };

            double exhaust = 1.0;
            if (!DA.GetData(12, ref exhaust)) { return; };

            List<string> slices = new List<string>();
            if (!DA.GetDataList(13, slices)) { return; };
             
            int venttype = 0;
            if (!DA.GetData(14,ref venttype)) { return; };

            //EXPORT GEOMETRY
            if (export)
            {
                Point3d zero = new Point3d(0, 0, 0); 


                string[] lines;
                //Get the Text for &HEAD
                List<string> headlist = new List<string>();
                headlist.Add(head);

                

                //Get the end time and write &TIME
                List<string> timelist = new List<string>();
                string timeline = "&TIME T_END=" + time.ToString() + "/";
                timelist.Add(timeline);

                //Get the number of frames and write &DUMP
                List<string> dumplist = new List<string>();
                string dumpline = "&DUMP NFRAMES=" + nframes.ToString() + ",PLOT3D_QUANTITY(1:5)='TEMPERATURE','U-VELOCITY','V-VELOCITY','W-VELOCITY','PRESSURE',DT_PL3D="+ nframes.ToString() + ",WRITE_XYZ=T/";
                dumplist.Add(dumpline);

                //Get bounding box and write &MESH
                //CURRENTLY ASSUMES BOTTOM CORNER AT 0,0,0!!
                var meshbox = mesh.GetBoundingBox(true);
                var meshcorner = meshbox.GetCorners();
                string meshline = "&MESH XB=" + (meshcorner[0][0]).ToString() + "," + (meshcorner[1][0]).ToString() + "," + (meshcorner[0][1]).ToString() + "," + (meshcorner[2][1]).ToString() + "," + (meshcorner[0][2]) + "," + (meshcorner[4][2]).ToString() + "," + " IJK=" + ijk[0].ToString() + "," + ijk[1].ToString() + "," + ijk[2].ToString() + " /";
                List<string> meshlist = new List<string>();
                meshlist.Add(meshline);

                //Get obstructions (walls, etc...) and write &OBST
                var obstlist = new List<string>();
                if (obsts.Count != 0)
                {
                    
                    foreach (double[] obst in obsts)
                    {
                        string obstline = "&OBST XB=" + obst[0].ToString() + "," + obst[1].ToString() + "," + obst[2].ToString() + "," + obst[3].ToString() + ",0," + obst[5].ToString() + " , SURF_ID='INERT' /";
                        obstlist.Add(obstline);
                    }
                }


                //Get inlets and write &VENT
                var inletlist = new List<string>();
                foreach (Surface inlet in inlets)
                {

                    double[] inletinfo = InletGeo.SurfaceArea(inlet);

                    string inletboxline = null;
                    string supplyline = null;
                    

                    switch (venttype)
                    {
                        

                        case 0: //Linear

                            inletboxline = $"&VENT XB={inletinfo[1]},{inletinfo[2]},{inletinfo[3]},{inletinfo[4]},{inletinfo[5]},{inletinfo[6]}, COLOR='GREEN', SURF_ID='supply' /";
                           
                            inletlist.Add(inletboxline);
                            
                            break;
                        case 1://Linear Louvered
                            inletboxline = $"&VENT XB={inletinfo[1]},{inletinfo[2]},{inletinfo[3]},{inletinfo[4]},{inletinfo[5]},{inletinfo[6]}, COLOR='GREEN', SURF_ID='supply' /";
                        
                            inletlist.Add(inletboxline);
                        
                            break;
                        case 2: // Square Louvered
                            List<string> inletlines = InletGeo.SplitSquare(inlet);
                            inletlist.AddRange(inletlines);
                            var hi = inletlist;

                            break;
                        case 3: //Swirl
                            List<string> inletlinesswirl = InletGeo.SplitSwirl(inlet);
                            inletlist.AddRange(inletlinesswirl);
                          
                            break;
                    }
                   


                }

                List<string> supplylist = new List<string>();
                double area = InletGeo.SurfaceArea(inlets[0])[0];


                switch (venttype)
                {


                    case 0: //Linear

                        
                        string supplyline = "&SURF ID = 'supply', VOLUME_FLOW = -" + supply.ToString() + " /";
                       
                        supplylist.Add(supplyline);
                        break;
                    case 1://Linear Louvered
                        
                        string supplylinelin = $"&SURF ID='supply', VOLUME_FLOW=-{supply}, VEL_T=0,-{supply / 0.09 * Math.Tan(45 * Math.PI / 180)} /";
                        supplylist.Add(supplylinelin);
                        break;
                    case 2: // Square Louvered
                       
                        List<string> supplylines = InletGeo.WriteSurfSquare(supply, area);
                        supplylist.AddRange(supplylines);
                       
                        break;
                    case 3: //Swirl
                       
                        List<string> supplylinesswirl = InletGeo.WriteSurfSwirl(supply, area);
                        supplylist.AddRange(supplylinesswirl);
                        break;
                }




                //Get outlets and write &VENT
                var outletlist = new List<string>();
                foreach (Surface outlet in outlets)
                {
                    BoundingBox outletbox = outlet.GetBoundingBox(true);
                    Point3d[] outletcorner = outletbox.GetCorners();
                    string outletboxline = $"&VENT XB={(outletcorner[0][0])},{(outletcorner[1][0])},{(outletcorner[0][1])},{(outletcorner[2][1])},{(outletcorner[0][2])},{(outletcorner[4][2])}, COLOR='RED', SURF_ID='exhaust' /";
                    outletlist.Add(outletboxline);
                }

                //Get supply and exhaust velocity and write &SURF
                //List<string> supplylist = new List<string>();
                //string supplyline = "&SURF ID = 'supply', VOLUME_FLOW = -" + supply.ToString() + " /";
                //supplylist.Add(supplyline);

                List<string> exhaustlist = new List<string>();
                string exhaustline = $"&SURF ID = 'exhaust', VOLUME_FLOW = {exhaust.ToString()} /";
                exhaustlist.Add(exhaustline);

                //Get slices lines
                var sliceslist = new List<string>();
                foreach (string slice in slices)
                {
                    sliceslist.Add(slice);
                }


                //Write & Tail
                string tail = "&TAIL /";
                List<string> taillist = new List<string>();
                taillist.Add(tail);

                var ht = headlist.Concat(timelist);
                var htd = ht.Concat(dumplist);
                var htdm = htd.Concat(meshlist);
                var htdmo = htdm.Concat(obstlist);
                var htdmoi = htdmo.Concat(inletlist);
                var htdmoio = htdmoi.Concat(outletlist);
                var htdmoios = htdmoio.Concat(supplylist);
                var htdmoiose = htdmoios.Concat(exhaustlist);
                var htdmoioses = htdmoiose.Concat(sliceslist);
                var htdmoiosest = htdmoioses.Concat(taillist);



                lines = htdmoiosest.ToArray();

                File.WriteAllLines(path, lines);
                export = false;

              

            }



        }




        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return GHWind.Properties.Resources.fire;
            }
        }



        public override Guid ComponentGuid
        {
            get { return new Guid("{35b400d4-976d-4d06-942c-d0c47973fcb6}"); }
        }
    }
}
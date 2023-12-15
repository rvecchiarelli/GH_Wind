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
            pManager.AddIntegerParameter("Case Index", "Index", "Index of the case to write as an integer", GH_ParamAccess.item);
            //2
            pManager.AddTextParameter("path", "path", "Path to the directory where files will be written. A subdirectory will be created with named with the current index. A .fds file will be created within this directory. If these already exsist they will be overwritten.", GH_ParamAccess.item);
            //3
            pManager.AddNumberParameter("End Time", "End Time", "End time as single number", GH_ParamAccess.item);
            //4
            pManager.AddNumberParameter("Initial Room Temperature", "Initial Temp", "Initial room temperature in °C", GH_ParamAccess.item);
            //5
            pManager.AddIntegerParameter("Number of Frames", "NFrames", "Frequency of file dumps.", GH_ParamAccess.item);
            //6
            pManager.AddPointParameter("origin", "origin", "Origin point that is the same as the origin input of Discr_Mesh.", GH_ParamAccess.item);
            //7
            pManager.AddBrepParameter("bounding box", "box", "Bounding box as brep for full mesh", GH_ParamAccess.item);
            //8
            pManager.AddTextParameter("IJK", "I J K", "Number of cells in the X, Y, and Z directions as a list.", GH_ParamAccess.list);
            //9
            pManager.AddGenericParameter("cubes as doubles", "cubes", "Obsacales (cubes as list of double[xmin, xmax, ymin, ymax, zmin, zmax] (Discr_Mesh output))", GH_ParamAccess.list);
            pManager[9].Optional = true;
            //10
            pManager.AddSurfaceParameter("Inlet Surfaces", "Inlet Surfaces", "List of inlet surfaces (rectangular)", GH_ParamAccess.list);
            //11
            pManager.AddSurfaceParameter("Outlet Surfaces", "Outlet Surfaces", "List of outlet surfaces (rectangular)", GH_ParamAccess.list);
            //12
            pManager.AddIntegerParameter("Vent Type", "Vent Type", "Coefficients for Vent Type. 0 = Linear; 1 = Louvered; 2 = Radial; 3 = Spiral.", GH_ParamAccess.item);
            //13
            pManager.AddNumberParameter("Supply Temperature", "Supply Temp", "Temperature of the supply air in °C", GH_ParamAccess.item);
            //14
            pManager.AddNumberParameter("Supply Velocity", "Supply Velocity", "Supply velocity in m/s", GH_ParamAccess.item);
            //15
            pManager.AddNumberParameter("Exhaust Velocity", "Exhaust Velocity", "Exhaust velocity in m/s", GH_ParamAccess.item);
            //16
            pManager.AddTextParameter("Slices", "Slices", "List of &SLCF lines", GH_ParamAccess.list);
            


        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
          
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            DA.GetData(0, ref export);

            string path = null;
            if (!DA.GetData(2, ref path)) { return; };

            Point3d origin = Point3d.Unset;
            if (!DA.GetData(6, ref origin)) { return; }

            Brep mesh = new Brep();
            if (!DA.GetData(7, ref mesh)) { return; }

            List<double[]> obsts = new List<double[]>();
            DA.GetDataList(9, obsts);

            List<Surface> inlets = new List<Surface>();
            if (!DA.GetDataList(10, inlets)) { return; };

            List<Surface> outlets = new List<Surface>();
            if (!DA.GetDataList(11, outlets)) { return; }

            double time = 60.0;
            if (!DA.GetData(3, ref time)) { return; };

            int nframes = 1000;
            if (!DA.GetData(5, ref nframes)) { return; };

            List<string> ijk = new List<string>();
            if (!DA.GetDataList(8, ijk)) { return; };

            double supply = 1.0;
            if (!DA.GetData(14, ref supply)) { return; };

            double exhaust = 1.0;
            if (!DA.GetData(15, ref exhaust)) { return; };

            List<string> slices = new List<string>();
            if (!DA.GetDataList(16, slices)) { return; };
             
            int venttype = 0;
            if (!DA.GetData(12,ref venttype)) { return; };

            int index = 0;
            if (!DA.GetData(1, ref index)) { return; };

            double inittmp = 20;
            if (!DA.GetData(4, ref inittmp)) { return; };

            double supplytmp = 20;
            if (!DA.GetData(13, ref supplytmp)) { return; };


            //EXPORT GEOMETRY
            if (export)
            {
                Point3d zero = new Point3d(0, 0, 0); 


                string[] lines;
                //Get the Text for &HEAD
                List<string> headlist = new List<string>();
                string ventname;
                if (venttype == 0)
                {
                    ventname = "Linear";
                }
                else if (venttype == 1)
                {
                    ventname = "Louvered";
                }
                else if (venttype == 2)
                {
                    ventname = "Louvered Square";
                }
                else
                {
                    ventname = "Swirl";
                }

                string head = $"&HEAD TITLE='Case: {index}, Vent Type:{ventname}', CHID = '{index}' / ";
                string misc = $"&MISC TMPA = {inittmp} /";
               
                headlist.Add(head);
                headlist.Add(misc);


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

                        string supplyline = $"&SURF ID = 'supply', VEL = -{supply.ToString()},TMP_FRONT= {supplytmp} /";
                        supplylist.Add(supplyline);
                        break;
                    case 1://Linear Louvered
                        
                        string supplylinelin = $"&SURF ID='supply', VEL=-{supply}, VEL_T=0,-{supply / 0.09 * Math.Tan(45 * Math.PI / 180)}, TMP_FRONT= {supplytmp} /";
                        supplylist.Add(supplylinelin);
                        break;
                    case 2: // Square Louvered
                       
                        List<string> supplylines = InletGeo.WriteSurfSquare(supply, supplytmp);
                        supplylist.AddRange(supplylines);
                        break;
                    case 3: //Swirl
                       
                        List<string> supplylinesswirl = InletGeo.WriteSurfSwirl(supply, supplytmp);
                        supplylist.AddRange(supplylinesswirl);
                        break;
                }




                //Get outlets and write &VENT
                var outletlist = new List<string>();
                foreach (Surface outlet in outlets)
                {
                    double[] outletcorner = InletGeo.SurfaceArea(outlet);
                    string outletboxline = $"&VENT XB={(outletcorner[1])},{(outletcorner[2])},{(outletcorner[3])},{(outletcorner[4])},{(outletcorner[5])},{(outletcorner[6])}, COLOR='RED', SURF_ID='exhaust' /";
                    outletlist.Add(outletboxline);
                }

                //Get supply and exhaust velocity and write &SURF
                //List<string> supplylist = new List<string>();
                //string supplyline = "&SURF ID = 'supply', VOLUME_FLOW = -" + supply.ToString() + " /";
                //supplylist.Add(supplyline);

                List<string> exhaustlist = new List<string>();
                string exhaustline = $"&SURF ID = 'exhaust', VEL = {exhaust.ToString()} /";
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

                string maindir = $@"{path}\{index.ToString()}";
                string filepath = $@"{maindir}\{index.ToString()}.fds";

                Directory.CreateDirectory(maindir);
                var currentfile = File.Create(filepath);
                currentfile.Close();
                File.WriteAllLines(filepath, lines);
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
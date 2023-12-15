using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
using System.Web;

namespace GHWind
{
    public class InletGeo
    {
       public static double[] SurfaceArea(Surface surf)
        {
            BoundingBox box = surf.GetBoundingBox(true);
            Point3d[] boxcorner = box.GetCorners();

            double[] surfinfo = new double[7];
            double area = 0; 

            var xmin = boxcorner[0][0];
            var xmax = boxcorner[1][0];
            var ymin = boxcorner[0][1];
            var ymax = boxcorner[2][1];
            var zmin = boxcorner[0][2];
            var zmax = boxcorner[4][2];

            if (Math.Abs(xmin - xmax) < 0.000001)
            {
                area = (ymax -ymin) * (zmax - zmin);
            }
            else if (Math.Abs(ymin - ymax) < 0.000001)
            {
                area = (zmax - zmin) * (xmax - xmin);
            }
            else
            {
                area = (xmax - xmin) * (ymax - ymin);
            }
           
            surfinfo[0] = area;
            surfinfo[1] = xmin;
            surfinfo[2] = xmax;
            surfinfo[3] = ymin;
            surfinfo[4] = ymax;
            surfinfo[5] = zmin;
            surfinfo[6] = zmax;

            return surfinfo;
        }

        public static List<string> SplitSwirl (Surface surf)
        {
            List<string> vents = new List<string>();
            List<double> areas = new List<double>();

            double xmin = SurfaceArea(surf)[1];
            
            double xmax = SurfaceArea(surf)[2];
            
            double ymin = SurfaceArea(surf)[3];
              
            double ymax = SurfaceArea(surf)[4];
            
            double zmin = SurfaceArea(surf)[5];
            
            double zmax = SurfaceArea(surf)[6];
            
            
            var dx = (xmax - xmin) / 3;
            var dy = (ymax - ymin) / 3;
            var dz = (zmax - zmin) / 3;



            if (dx == 0)
            {
                for (int i = 1; i <= 9; i++)
                {
                    string vent = $"&VENT XB ={xmin},{xmin},{ymin + (i - 1) % 3 * dy},{ymin + (i % 3) * dy},{zmin + (i - 1) / 3 * dz},{zmin + i / 3 * dz}, COLOR ='GREEN', SURF_ID='supply {i}' /";
                    
                    
                    vents.Add(vent);
                }

            }

            else if (dy == 0)
            {
                for (int i = 1; i <= 9; i++)
                {
                    string vent = $"&VENT XB ={xmin + (i - 1) % 3 * dx},{xmin + (i % 3) * dx},{ymin},{ymin},{zmin + (i - 1) / 3 * dz},{zmin + i / 3 * dz}, COLOR = 'GREEN', SURF_ID='supply {i}' /";
                
                    vents.Add(vent);
                }
            }

            else
            {
                for (int i = 1; i <= 9; i++)
                {
                    string vent = $"&VENT XB ={xmin + (i - 1) / 3 * dx},{xmin + i / 3 * dx},{ymin + (i - 1) / 3 * dy},{ymin + i / 3 * dy},{zmin},{zmin}, COLOR = 'GREEN', SURF_ID='supply {i}' /";
                    
                    vents.Add(vent);
                }
            }

           

            return vents;
        }

        public static List<string> WriteSurfSwirl(double supply, double supplytmp)
        {
            List<string> surfs = new List<string>();

            

            string surf1 = $"&SURF ID='supply 1', VEL =-{supply}, VEL_T = -{supply *Math.Sin((45*(Math.PI/180))) * Math.Cos((0 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((0 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf1);                      
            string surf2 = $"&SURF ID='supply 2', VEL =-{supply}, VEL_T = -{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((45 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((45 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf2);                      
            string surf3 = $"&SURF ID='supply 3', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((90 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((90 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf3);                      
            string surf4 = $"&SURF ID='supply 4', VEL =-{supply}, VEL_T = -{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((45 * (Math.PI / 180)))},-{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((45 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf4);                      
            string surf5 = $"&SURF ID='supply 5', VEL = 0 /";
            surfs.Add(surf5);                      
            string surf6 = $"&SURF ID='supply 6', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((45 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((45 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf6);                      
            string surf7 = $"&SURF ID='supply 7', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((90 * (Math.PI / 180)))},-{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((90 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf7);                      
            string surf8 = $"&SURF ID='supply 8', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((45 * (Math.PI / 180)))},-{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((45 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf8);                      
            string surf9 = $"&SURF ID='supply 9', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((0 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((0 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf9);


            return surfs;
        }

        public static List<string> SplitSquare(Surface surf)
        {
            List<string> vents = new List<string>();
            List<double> areas = new List<double>();

            double xmin = SurfaceArea(surf)[1];

            double xmax = SurfaceArea(surf)[2];

            double ymin = SurfaceArea(surf)[3];

            double ymax = SurfaceArea(surf)[4];

            double zmin = SurfaceArea(surf)[5];

            double zmax = SurfaceArea(surf)[6];


            var dx = (xmax - xmin) / 6;
            var dy = (ymax - ymin) / 6;
            var dz = (zmax - zmin) / 6;

            if (dx == 0)
            {
                string vent1 = $"&VENT XB = {xmin},{xmin},{ymin},{ymin + dy},{zmin},{zmax}, COLOR = 'GREEN', SURF_ID ='supply 1' /";
                vents.Add(vent1);
                string vent2 = $"&VENT XB = {xmin},{xmin},{ymin + dy}, {ymax - dy}, {zmax - dz}, {zmax}, COLOR = 'GREEN', SURF_ID ='supply 2' /";
                vents.Add(vent2);
                string vent3 = $"&VENT XB = {xmin},{xmin},{ymax - dy},{ymax},{zmin},{zmax}, COLOR = 'GREEN', SURF_ID ='supply 3' /";
                vents.Add(vent3);
                string vent4 = $"&VENT XB = {xmin},{xmin},{ymin + dy},{ymax - dy},{zmin},{zmin + dz}, COLOR = 'GREEN', SURF_ID ='supply 4' /";
                vents.Add(vent4);
                string vent5 = $"&VENT XB = {xmin},{xmin}, {ymin + dy}, {ymin + 2*dy}, {zmin + 2*dz}, {zmax - 2*dz}, COLOR = 'GREEN', SURF_ID ='supply 5' /";
                vents.Add(vent5);
                string vent6 = $"&VENT XB = {xmin},{xmin}, {ymin + dy}, {ymax-dy}, {zmax - 2*dz}, {zmax - dz}, COLOR = 'GREEN', SURF_ID ='supply 6' /";
                vents.Add(vent6);
                string vent7 = $"&VENT XB = {xmin},{xmin}, {ymax - 2*dy}, {ymax - dy}, {zmin + 2 * dz}, {zmax - 2 * dz}, COLOR = 'GREEN', SURF_ID ='supply 7' /";
                vents.Add(vent7);
                string vent8 = $"&VENT XB = {xmin},{xmin},{ymin + dy}, {ymax - dy},{zmin + dy}, {zmin + 2*dy}, COLOR = 'GREEN', SURF_ID ='supply 8' /";
                vents.Add(vent8);

            }

            else if (dy == 0)
            {
                string vent1 = $"&VENT XB = {xmin},{xmin + dx},{ymin},{ymin},{zmin},{zmax}, COLOR = 'GREEN', SURF_ID ='supply 1' /";
                vents.Add(vent1);
                string vent2 = $"&VENT XB = {xmin + dx},{xmax - dx},{ymin}, {ymin}, {zmax - dz}, {zmax}, COLOR = 'GREEN', SURF_ID ='supply 2' /";
                vents.Add(vent2);
                string vent3 = $"&VENT XB = {xmax - dx},{xmax},{ymin},{ymin},{zmin},{zmax}, COLOR = 'GREEN', SURF_ID ='supply 3' /";
                vents.Add(vent3);
                string vent4 = $"&VENT XB = {xmin +dx},{xmax - dx},{ymin},{ymin},{zmin},{zmin + dz}, COLOR = 'GREEN', SURF_ID ='supply 4' /";
                vents.Add(vent4);
                string vent5 = $"&VENT XB = {xmin + dx},{xmin + 2*dx}, {ymin}, {ymin}, {zmin + 2 * dz}, {zmax - 2 * dz}, COLOR = 'GREEN', SURF_ID ='supply 5' /";
                vents.Add(vent5);
                string vent6 = $"&VENT XB = {xmin + dx},{xmax - dx}, {ymin}, {ymin}, {zmax - 2 * dz}, {zmax - dz}, COLOR = 'GREEN', SURF_ID ='supply 6' /";
                vents.Add(vent6);
                string vent7 = $"&VENT XB = {xmax - 2*dx},{xmax - dx}, {ymin}, {ymin}, {zmin + 2 * dz}, {zmax - 2 * dz}, COLOR = 'GREEN', SURF_ID ='supply 7' /";
                vents.Add(vent7);
                string vent8 = $"&VENT XB = {xmin + dx},{xmax - dx},{ymin}, {ymin},{zmin + dy}, {zmin + 2 * dy}, COLOR = 'GREEN', SURF_ID ='supply 8' /";
                vents.Add(vent8);

            }

            else
            {
                string vent1 = $"&VENT XB = {xmin}, {xmin + dx}, {ymin}, {ymax}, {zmin}, {zmin}, COLOR = 'GREEN', SURF_ID ='supply 1' /";
                vents.Add(vent1);
                string vent2 = $"&VENT XB = {xmin+ dx}, {xmax - dx}, {ymax - dy}, {ymax}, {zmin}, {zmin}, COLOR = 'GREEN', SURF_ID ='supply 2' /";
                vents.Add(vent2);
                string vent3 = $"&VENT XB = {xmax-dx}, {xmax}, {ymin}, {ymax},{zmin}, {zmin}, COLOR = 'GREEN', SURF_ID ='supply 3' /";
                vents.Add(vent3);
                string vent4 = $"&VENT XB = {xmin + dx}, {xmax - dx}, {ymin}, {ymin +dy},{zmin}, {zmin}, COLOR = 'GREEN', SURF_ID ='supply 4' /";
                vents.Add(vent4);
                string vent5 = $"&VENT XB = {xmin + dx}, {xmin + 2*dx}, {ymin + 2*dy}, {ymax - 2*dy}, {zmin}, {zmin}, COLOR = 'GREEN', SURF_ID ='supply 5' /";
                vents.Add(vent5);
                string vent6 = $"&VENT XB = {xmin+ dx}, {xmax - dx}, {ymax - 2 * dy}, {ymax - dy}, {zmin}, {zmin},  COLOR = 'GREEN', SURF_ID ='supply 6' /";
                vents.Add(vent6 );
                string vent7 = $"&VENT XB = {xmax - 2*dx}, {xmax - dx}, {ymin + 2 * dy}, {ymax - 2 * dy}, {zmin}, {zmin}, COLOR = 'GREEN', SURF_ID ='supply 7' /";
                vents.Add(vent7); 
                string vent8 = $"&VENT XB = {xmin + dx}, {xmax - dx}, {ymin + dy }, {ymin + 2* dy}, {zmin}, {zmin}, COLOR = 'GREEN', SURF_ID ='supply 8' /";
                vents.Add(vent8);

            }


            return vents;
        }

        public static List<string> WriteSurfSquare(double supply, double supplytmp)
        {
            List<string> surfs = new List<string>();

                      

            string surf1 = $"&SURF ID='supply 1', VEL =-{supply}, VEL_T = -{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((0 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((0 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf1);                      
            string surf2 = $"&SURF ID='supply 2', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((90 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((90 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf2);                      
            string surf3 = $"&SURF ID='supply 3', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((0 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((0 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf3);                      
            string surf4 = $"&SURF ID='supply 4', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((90 * (Math.PI / 180)))},-{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((90 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf4);                      
            string surf5 = $"&SURF ID='supply 5', VEL =-{supply}, VEL_T = -{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((0 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((0 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf5);                      
            string surf6 = $"&SURF ID='supply 6', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((90 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((90 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf6);                      
            string surf7 = $"&SURF ID='supply 7', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((0 * (Math.PI / 180)))},{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((0 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf7);                      
            string surf8 = $"&SURF ID='supply 8', VEL =-{supply}, VEL_T = {supply * Math.Sin((45 * (Math.PI / 180))) * Math.Cos((90 * (Math.PI / 180)))},-{supply * Math.Sin((45 * (Math.PI / 180))) * Math.Sin((90 * (Math.PI / 180)))}, TMP_FRONT= {supplytmp} /";
            surfs.Add(surf8);


            return surfs;
        }

    }
}




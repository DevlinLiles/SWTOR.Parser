﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SWTOR.Parser;
using System.IO;
using System.Web.Script.Serialization;

namespace SWTOR.Web.Controllers
{
    public class APIController : Controller
    {
        [HttpPost]
        public ActionResult AnalyzeDPS(string combatLog)
        {
            var parser = new SWTOR.Parser.Parser();
            var analyzer = new Analyzer();
            var log = parser.Parse(new StringReader(combatLog));
            var analyzedData = analyzer.AnalyzeDpsPerCharacter(log);

            var outVal = new Dictionary<string, IEnumerable<IEnumerable<int>>>();
            var q = from point in analyzedData
                    group point by point.character into charPoints
                    select new { character = charPoints.Key, points = charPoints.ToList() };

            foreach (var i in q)
            {
                var dataPoints = new List<List<int>>();
                foreach (var point in i.points)
                {
                    dataPoints.Add(new List<int> { point.interval, point.damage });
                }
                outVal.Add(i.character, dataPoints.Select(m => m.ToArray()).ToArray());
            }

            var ser = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue };
            return new ContentResult { Content = ser.Serialize(outVal), ContentType = "application/json" };
        }

        [HttpPost]
        public ActionResult Parse()
        {
            object returnData = null;

            var parser = new SWTOR.Parser.Parser();
            if (Request.Files.Count > 0)
            {
                Stream stream = Request.Files["combatLog"].InputStream;
                returnData = parser.Parse(new StreamReader(stream));
            }

            return Json(returnData);
        }

        [HttpPost]
        public ActionResult ParseText(string combatLog)
        {
            object returnData = null;

            var parser = new SWTOR.Parser.Parser();
            returnData = parser.Parse(new StringReader(combatLog));

            var ser = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue };
            return new ContentResult { Content = ser.Serialize(returnData), ContentType = "application/json" };
        }


    }
}
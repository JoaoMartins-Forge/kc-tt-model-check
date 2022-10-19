using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using DesignAutomationFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;

namespace Autodesk.Forge.Sample.DesignAutomation.Revit
{
	[Transaction(TransactionMode.Manual)]
	[Regeneration(RegenerationOption.Manual)]
	public class Commands : IExternalDBApplication
	{

		public ExternalDBApplicationResult OnStartup(ControlledApplication application)
		{
			DesignAutomationBridge.DesignAutomationReadyEvent += HandleDesignAutomationReadyEvent;
			return ExternalDBApplicationResult.Succeeded;
		}

		private void HandleDesignAutomationReadyEvent(object sender, DesignAutomationReadyEventArgs e)
		{
			LogTrace("Design Automation Ready event triggered...");
			e.Succeeded = true;
			Checkmodel(e.DesignAutomationData.RevitDoc);
		}

		private void Checkmodel(Document revitDoc)
		{
			Console.WriteLine("Acquiring Schedules");
			List<ViewSchedule> schedules = new FilteredElementCollector(revitDoc).OfClass(typeof(ViewSchedule)).WhereElementIsNotElementType().Cast<ViewSchedule>().ToList();
			Console.WriteLine($"{schedules.Count} schedules found!");

			Console.WriteLine("Acquiring Viewports!");
			List<Viewport> viewports = new FilteredElementCollector(revitDoc).OfClass(typeof(Viewport)).WhereElementIsNotElementType().Cast<Viewport>().ToList();
			Console.WriteLine($"{viewports.Count} viewports found!");

			Console.WriteLine("Acquiring ViewSheets!");
			List<ViewSheet> viewSheets = new FilteredElementCollector(revitDoc).OfClass(typeof(ViewSheet)).WhereElementIsNotElementType().Cast<ViewSheet>().ToList();
			Console.WriteLine($"{viewSheets.Count} viewsheets found!");

			Console.WriteLine("Acquiring Views!");
			List<View> views = new FilteredElementCollector(revitDoc).OfClass(typeof(View)).WhereElementIsNotElementType().Cast<View>().ToList();
			Console.WriteLine($"{views.Count} views found!");

			Console.WriteLine("Acquiring Family Instances!");
			List<FamilyInstance> familyInstances = new FilteredElementCollector(revitDoc).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
			Console.WriteLine($"{familyInstances.Count} Family Instances found!");

			Console.WriteLine("Acquiring Imported Instances!");
			List<ImportInstance> importedInstances = new FilteredElementCollector(revitDoc).OfClass(typeof(ImportInstance)).WhereElementIsNotElementType().Cast<ImportInstance>().ToList();
			Console.WriteLine($"{importedInstances.Count} Family Imported found!");

			dynamic duplicatedInstances = getDuplicatedInstances(familyInstances);

			int viewsNotInViewports = getViewsNotOnViewport(views, viewSheets, viewports);

			dynamic urnResult = new JObject();
			urnResult.Add(new JProperty("duplicated instances", duplicatedInstances));
			urnResult.Add(new JProperty("views not on sheets", viewsNotInViewports));
			urnResult.Add(new JProperty("schedules", schedules.Count));
			urnResult.Add(new JProperty("warnings", revitDoc.GetWarnings().ToList().Count));

			using (StreamWriter file = File.CreateText("result.json"))
			using (JsonTextWriter writer = new JsonTextWriter(file))
			{
				urnResult.WriteTo(writer);
			}

		}

		private dynamic getDuplicatedInstances(List<FamilyInstance> familyInstances)
		{
			List<FamilySymbol> symbols = familyInstances.Select(f => f.Symbol).ToList();
			JObject familySymbols = new JObject();
			foreach (FamilySymbol symbol in symbols)
			{
				if (familySymbols.Property(symbol.FamilyName) == null)
				{
					familySymbols.Add(new JProperty(symbol.FamilyName, 0));
				}
				familySymbols.Property(symbol.FamilyName).Value = (int)familySymbols.Property(symbol.FamilyName).Value + 1;
			}

			int duplicateFamilySymbols = familySymbols.Properties().Where(p => (int)p.Value > 1).ToList().Count;

			return duplicateFamilySymbols;
		}

		private int getViewsNotOnViewport(List<View> views, List<ViewSheet> viewSheets, List<Viewport> viewports)
		{
			List<ElementId> viewIds = viewports.Select(v => v.ViewId).ToList();
			List<View> viewsNotInViewports = views.Where(v => !viewIds.Contains(v.Id)).ToList();
			return viewsNotInViewports.Count;
		}

		public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
		{
			return ExternalDBApplicationResult.Succeeded;
		}

		/// <summary>
		/// This will appear on the Design Automation output
		/// </summary>
		private static void LogTrace(string format, params object[] args) { System.Console.WriteLine(format, args); }
	}
}
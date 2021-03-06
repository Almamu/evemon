﻿using System;
using System.Collections.Generic;
using System.Text;
using EVEMon.Common.SettingsObjects;
using System.IO;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Serialization.Exportation;
using System.IO.Compression;
using System.Windows.Forms;

namespace EVEMon.Common
{
    public enum PlanFormat
    {
        None = 0,
        Emp = 1,
        Xml = 2,
        Text = 3
    }


    public static class PlanExporter
    {
        /// <summary>
        /// Exports the plan under a text format.
        /// </summary>
        /// <param name="planToExport"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string ExportAsText(Plan planToExport, PlanExportSettings settings)
        {
            var plan = new PlanScratchpad(planToExport.Character, planToExport);
            plan.Sort(planToExport.SortingPreferences);
            plan.UpdateStatistics();

            var builder = new StringBuilder();
            var timeFormat = DescriptiveTextOptions.FullText | DescriptiveTextOptions.IncludeCommas | DescriptiveTextOptions.SpaceText;
            var character = (Character)plan.Character;

            // Initialize constants
            string lineFeed = "";
            string boldStart = "";
            string boldEnd = "";

            switch (settings.Markup)
            {
                default:
                    break;
                case MarkupType.Forum:
                    boldStart = "[b]";
                    boldEnd = "[/b]";
                    break;
                case MarkupType.Html:
                    lineFeed = "<br />";
                    boldStart = "<b>";
                    boldEnd = "</b>";
                    break;
            }

            // Header
            if (settings.IncludeHeader)
            {
                builder.Append(boldStart);
                if (settings.ShoppingList)
                {
                    builder.Append("Shopping list for ").Append(character.Name);
                }
                else
                {
                    builder.Append("Skill plan for ").Append(character.Name);
                }
                builder.Append(boldEnd);
                builder.AppendLine(lineFeed);
                builder.AppendLine(lineFeed);
            }

            // Scroll through entries
            int index = 0;
            DateTime endTime = DateTime.Now;
            foreach (PlanEntry entry in plan)
            {
                // Remapping point
                if (!settings.ShoppingList && entry.Remapping != null)
                {
                    builder.Append("***").Append(entry.Remapping.ToString()).Append("***");
                    builder.AppendLine(lineFeed);
                }

                // Skip is we're only build a shopping list
                bool shoppingListCandidate = !(entry.CharacterSkill.IsKnown || entry.Level != 1 || entry.CharacterSkill.IsOwned);
                if (settings.ShoppingList && !shoppingListCandidate) continue;

                // Entry's index
                index++;
                if (settings.EntryNumber) builder.Append(index.ToString()).Append(". ");

                // Name
                builder.Append(boldStart);

                if (settings.Markup == MarkupType.Html) builder.Append("<a href=\"\" onclick=\"CCPEVE.showInfo(").Append(entry.Skill.ID.ToString()).Append(")\">");
                builder.Append(entry.Skill.Name);
                if (settings.Markup == MarkupType.Html) builder.Append("</a>");

                if (!settings.ShoppingList) builder.Append(' ').Append(Skill.GetRomanForInt(entry.Level));

                builder.Append(boldEnd);

                // Training time
                if (settings.EntryTrainingTimes || settings.EntryStartDate || settings.EntryFinishDate || (settings.EntryCost && shoppingListCandidate))
                {
                    builder.Append(" (");
                    bool needComma = false;

                    // Training time
                    if (settings.EntryTrainingTimes)
                    {
                        needComma = true;
                        builder.Append(Skill.TimeSpanToDescriptiveText(entry.TrainingTime, timeFormat));
                    }

                    // Training start date
                    if (settings.EntryStartDate)
                    {
                        if (needComma) builder.Append("; ");
                        needComma = true;

                        builder.Append("Start: ").Append(entry.StartTime.ToString());
                    }

                    // Training end date
                    if (settings.EntryFinishDate)
                    {
                        if (needComma) builder.Append("; ");
                        needComma = true;

                        builder.Append("Finish: ").Append(entry.EndTime.ToString());
                    }

                    // Skill cost
                    if (settings.EntryCost && shoppingListCandidate)
                    {
                        if (needComma) builder.Append("; ");
                        needComma = true;

                        builder.Append(entry.Skill.FormattedCost + " ISK");
                    }

                    builder.Append(')');
                }
                builder.AppendLine(lineFeed);

                // End time
                endTime = entry.EndTime;
            }

            // Footer
            if (settings.FooterCount || settings.FooterTotalTime || settings.FooterDate || settings.FooterCost)
            {
                builder.AppendLine(lineFeed);
                bool needComma = false;

                // Skills count
                if (settings.FooterCount)
                {
                    builder.Append(boldStart).Append(index.ToString()).Append(boldEnd);
                    builder.Append((index == 1 ? " skill" : " skills"));
                    needComma = true;
                }

                // Plan's training duration
                if (settings.FooterTotalTime)
                {
                    if (needComma) builder.Append("; ");
                    needComma = true;

                    builder.Append("Total time: ");
                    builder.Append(boldStart);
                    builder.Append(Skill.TimeSpanToDescriptiveText(plan.GetTotalTime(null, true), timeFormat));
                    builder.Append(boldEnd);
                }

                // End training date
                if (settings.FooterDate)
                {
                    if (needComma) builder.Append("; ");
                    needComma = true;

                    builder.Append("Completion: ");
                    builder.Append(boldStart).Append(endTime.ToString()).Append(boldEnd);
                }

                // Total books cost
                if (settings.FooterCost)
                {
                    if (needComma) builder.Append("; ");
                    needComma = true;

                    builder.Append("Cost: ");
                    builder.Append(boldStart);
                    builder.Append((plan.TotalBooksCost == 0 ? "0 ISK" : String.Format("{0:0,0,0} ISK", plan.TotalBooksCost)));
                    builder.Append(boldEnd);
                }

                // Warning about skill costs
                builder.AppendLine(lineFeed);
                if (settings.FooterCost || settings.EntryCost)
                {
                    builder.Append("N.B. Skill costs are based on CCP's database and are indicative only");
                    builder.AppendLine(lineFeed);
                }
            }

            // Returns the text representation.
            return builder.ToString();
        }

        /// <summary>
        /// Exports the plan under a XML format.
        /// </summary>
        /// <param name="planToExport"></param>
        /// <returns></returns>
        public static string ExportAsXML(Plan plan)
        {
            // Generates a settings plan and transforms it to an output plan
            var serial = plan.Export();
            var output = new OutputPlan { Name = serial.Name, Owner = serial.Owner, Revision = Settings.Revision };
            output.Entries.AddRange(serial.Entries);

            // Serializes to XML document and gets a string representation
            var doc = Util.SerializeToXmlDocument(typeof(OutputPlan), output);
            return Util.GetXMLStringRepresentation(doc);
        }

        /// <summary>
        /// Imports a <see cref="SerializablePlan"/> from the given filename. Works with old and new formats.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static SerializablePlan ImportFromXML(String filename)
        {
            SerializablePlan result = null;
            try
            {
                // Is the format compressed ? 
                if (filename.EndsWith(".emp"))
                {
                    string tempFile = Util.UncompressToTempFile(filename);
                    return ImportFromXML(tempFile);
                }

                // Reads the revision number from the file
                int revision = Util.GetRevisionNumber(filename);

                // Old format
                if (revision == 0)
                {
                    result = Util.DeserializeXML<SerializablePlan>(filename, Util.LoadXSLT(Properties.Resources.SettingsAndPlanImport));
                }
                // New format
                else
                {
                    result = Util.DeserializeXML<OutputPlan>(filename);
                }
            }
            catch (UnauthorizedAccessException exc)
            {
                MessageBox.Show("Couldn't read the given file, access was denied. Maybe the directory was under synchronization.");
                ExceptionHandler.LogException(exc, true);
            }
            catch (InvalidDataException exc)
            {
                MessageBox.Show("The file seems to be corrupted, wrong gzip format.");
                ExceptionHandler.LogException(exc, true);
            }

            if (result == null)
            {
                MessageBox.Show("There was a problem with the format of the document.");
            }

            return result;
        }
    }
}

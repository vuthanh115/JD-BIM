#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using kaka.Structure.Structural.App.Models.PrimaveraP6;
using kaka.Structure.Structural.App.Services.Database;

namespace kaka.Structure.Structural.App.Services.PrimaveraP6;

public static class PrimaveraP6ExportService
{
  public static readonly Dictionary<string, (string Name, string Resource, string CostAcc, int DefaultTasks, int Duration)> ComponentDefinitions =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["01.Coc"] = ("Công tác Cọc (Piles & Pile Caps)", "RES-PILE (Robot ép & Cọc D400)", "CA-PILE-00", 4, 10),
      ["02.Mong"] = ("Công tác Móng (Foundations)", "RES-CONC (Bê tông B25), RES-STEEL", "CA-CONC-01", 5, 12),
      ["03.DaKieng"] = ("Công tác Đà kiềng (Tie Beams)", "RES-FORMWORK (Ván phủ phim), RES-STEEL", "CA-FORM-03", 6, 8),
      ["04.Nen"] = ("Công tác Nền & Lớp lót (Slab on Grade)", "RES-CONC, Đá Base, Màng PVC", "CA-CONC-01", 16, 15),
      ["05.Cot"] = ("Công tác Cột kết cấu (Columns)", "RES-CONC (B25), Thép phân tầng F1-FN", "CA-STEEL-02", 5, 10),
      ["06.Vach"] = ("Công tác Vách thang máy (Shear Walls)", "RES-CONC (B25), Ván khuôn vách", "CA-FORM-03", 4, 8),
      ["07.Dam"] = ("Công tác Dầm kết cấu (Framing Beams)", "RES-CONC, Ván khuôn đáy & thành bên", "CA-CONC-01", 4, 9),
      ["08.San"] = ("Công tác Sàn kết cấu (Structural Slabs)", "RES-CONC, Thép sàn 2 lớp, Giàn giáo", "CA-CONC-01", 4, 12),
      ["09.CauThang"] = ("Công tác Cầu thang (Stairs)", "RES-LABOR (Đội thợ), Thép bản thang", "CA-DIRECT", 4, 6),
      ["10.TamCap"] = ("Công tác Tam cấp (Entrance Steps)", "RES-CONC, Ván khuôn khối tam cấp", "CA-DIRECT", 2, 4),
      ["11.Ramp"] = ("Công tác Ramp dốc (Loading Ramps)", "RES-CONC, Cắt rãnh chống trượt", "CA-CONC-01", 3, 5),
      ["12.Pit"] = ("Công tác Hố Pit (Elevator Pits)", "RES-CONC, Chống thấm tinh thể thẩm thấu", "CA-CONC-01", 3, 4),
      ["13.Dock"] = ("Công tác Sàn Dock (Loading Docks)", "RES-CONC, Bản thép chôn âm bảo vệ góc", "CA-DIRECT", 3, 5),
      ["14.CauKienPhu"] = ("Công tác Cấu kiện phụ (Secondary Works)", "RES-CONC, Vữa không co ngót Sika", "CA-DIRECT", 3, 4)
    };

  private static int GetCount(Dictionary<string, int> dict, string key)
  {
    return dict.TryGetValue(key, out var val) ? val : 0;
  }

  public static List<P6WbsSummaryItem> BuildWbsSummaries(Document doc)
  {
    var boqItems = RevitBoQExtractor.ExtractBoQ(doc);
    var countByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    foreach (var item in boqItems)
    {
      var code = item.CategoryCode;
      if (!string.IsNullOrEmpty(code))
      {
        countByCode[code] = GetCount(countByCode, code) + 1;
      }
    }

    var result = new List<P6WbsSummaryItem>();

    foreach (var kvp in ComponentDefinitions)
    {
      var compCode = kvp.Key;
      var def = kvp.Value;
      var elemCount = GetCount(countByCode, compCode);

      // Nếu không map được theo mã chính xác, quét theo tên danh mục
      if (elemCount == 0)
      {
        if (compCode == "02.Mong") elemCount = GetCount(countByCode, "02.Móng");
        else if (compCode == "03.DaKieng") elemCount = GetCount(countByCode, "03.Đà Kiềng");
        else if (compCode == "04.Nen") elemCount = GetCount(countByCode, "04.Nền");
        else if (compCode == "05.Cot") elemCount = GetCount(countByCode, "05.Cột");
        else if (compCode == "07.Dam") elemCount = GetCount(countByCode, "07.Dầm");
        else if (compCode == "08.San") elemCount = GetCount(countByCode, "08.Sàn");
      }

      result.Add(new P6WbsSummaryItem
      {
        ComponentCode = compCode,
        ComponentName = def.Name,
        WbsCode = $"WBS-KC.{compCode}",
        ElementCount = elemCount,
        TaskCount = def.DefaultTasks,
        PrimaryResource = def.Resource,
        CostAccount = def.CostAcc,
        DurationDays = def.Duration
      });
    }

    return result;
  }

  public static void ExportToCsv(string csvPath, string projectName, List<P6WbsSummaryItem> summaries)
  {
    var sb = new StringBuilder();
    // Header format chuẩn Primavera P6 Spreadsheet Import
    sb.AppendLine("Activity ID,Activity Name,WBS Code,Original Duration (Days),Primary Resource,Cost Account,Status,Notes");

    int actCounter = 1000;
    foreach (var item in summaries)
    {
      var compShort = item.ComponentCode.Replace(".", "");

      // Tạo các activity đại diện theo chuẩn phân bổ
      var taskTypes = new List<(string NameSuffix, int DurationDays)>
      {
        ("Thi công cốt thép & đệm lót", Math.Max(2, (int)(item.DurationDays * 0.35))),
        ("Lắp dựng ván khuôn & gông giằng", Math.Max(2, (int)(item.DurationDays * 0.25))),
        ("Đổ bê tông & nghiệm thu", Math.Max(1, (int)(item.DurationDays * 0.2))),
        ("Bảo dưỡng & hoàn thiện bề mặt", Math.Max(1, (int)(item.DurationDays * 0.2)))
      };

      for (int i = 0; i < taskTypes.Count; i++)
      {
        actCounter += 10;
        var actCode = $"ACT-KC-{compShort}-{actCounter}";
        var actName = $"{item.ComponentName} - {taskTypes[i].NameSuffix}";
        var duration = taskTypes[i].DurationDays;
        var notes = $"Revit Elements: {item.ElementCount}; Project: {projectName}";

        sb.AppendLine($"\"{actCode}\",\"{actName}\",\"{item.WbsCode}\",{duration},\"{item.PrimaryResource}\",\"{item.CostAccount}\",\"Not Started\",\"{notes}\"");
      }
    }

    File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
  }

  public static void ExportToXer(string xerPath, string projectName, List<P6WbsSummaryItem> summaries)
  {
    var nowStr = DateTime.Now.ToString("yyyy-MM-dd");
    var startDate = DateTime.Now.ToString("yyyy-MM-dd 08:00");
    var endDate = DateTime.Now.AddMonths(6).ToString("yyyy-MM-dd 17:00");

    var sb = new StringBuilder();
    sb.AppendLine($"ERMHDR\t23.12\t{nowStr}\tPROJECT\tPROJECT MANAGER\tADMIN\tkaka.Structure");

    // Table: PROJECT
    sb.AppendLine("%T\tPROJECT");
    sb.AppendLine("%F\tproj_id\tacct_id\torig_proj_id\tplan_start_date\tplan_end_date\tproj_short_name\tproj_title");
    sb.AppendLine($"%R\t1\t1\t1\t{startDate}\t{endDate}\tPRJ-KAKA-BIM\t{projectName} (Kaka BIM-P6 Integration)");

    // Table: PROJWBS
    sb.AppendLine("%T\tPROJWBS");
    sb.AppendLine("%F\twbs_id\tproj_id\tparent_wbs_id\twbs_short_name\twbs_name\tseq_num");
    sb.AppendLine("%R\t100\t1\t\tPROJ-ROOT\tToàn Bộ Dự Án (Project Root)\t1");
    sb.AppendLine("%R\t110\t1\t100\tWBS-KC\tPhân Hệ Kết Cấu Kaka (Structural Works)\t2");

    int wbsIdCounter = 120;
    var wbsMap = new Dictionary<string, int>();

    foreach (var item in summaries)
    {
      wbsIdCounter++;
      wbsMap[item.ComponentCode] = wbsIdCounter;
      sb.AppendLine($"%R\t{wbsIdCounter}\t1\t110\t{item.WbsCode}\t{item.ComponentCode} - {item.ComponentName}\t{wbsIdCounter - 100}");
    }

    // Table: TASK
    sb.AppendLine("%T\tTASK");
    sb.AppendLine("%F\ttask_id\tproj_id\twbs_id\ttask_code\ttask_name\ttarget_drtn_hr_cnt\ttask_type\tstatus_code");

    int actIdCounter = 1000;
    foreach (var item in summaries)
    {
      int currentWbsId = wbsMap[item.ComponentCode];
      var compShort = item.ComponentCode.Replace(".", "");

      var taskTypes = new List<(string NameSuffix, int DurationDays)>
      {
        ("Thi công cốt thép & đệm lót", Math.Max(2, (int)(item.DurationDays * 0.35))),
        ("Lắp dựng ván khuôn & gông giằng", Math.Max(2, (int)(item.DurationDays * 0.25))),
        ("Đổ bê tông & nghiệm thu", Math.Max(1, (int)(item.DurationDays * 0.2))),
        ("Bảo dưỡng & hoàn thiện bề mặt", Math.Max(1, (int)(item.DurationDays * 0.2)))
      };

      foreach (var t in taskTypes)
      {
        actIdCounter += 10;
        var actCode = $"ACT-KC-{compShort}-{actIdCounter}";
        var actName = $"{item.ComponentName} - {t.NameSuffix}";
        var durationHours = t.DurationDays * 8;

        sb.AppendLine($"%R\t{actIdCounter}\t1\t{currentWbsId}\t{actCode}\t{actName}\t{durationHours}\tTT_Task\tTK_NotStart");
      }
    }

    var utf8WithoutBom = new UTF8Encoding(false);
    File.WriteAllText(xerPath, sb.ToString(), utf8WithoutBom);
  }
}

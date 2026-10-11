#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

  public static readonly Dictionary<string, (string Name, string Resource, string CostAcc, int DefaultTasks, int Duration)> ArchComponentDefinitions =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["KT.01.TuongXay"] = ("Công tác Tường xây (Brick Walls & Partitions)", "RES-BRICK (Gạch tuynel & Vữa M75)", "CA-ARCH-WALL", 4, 15),
      ["KT.02.TratBa"] = ("Công tác Trát bả tường & trần (Plastering)", "RES-PLASTER (Thợ trát & Vữa M75)", "CA-ARCH-FINISH", 3, 12),
      ["KT.03.OpLat"] = ("Công tác Ốp lát gạch & đá (Tiling & Flooring)", "RES-TILE (Gạch Granite 600x600 & Keo dán)", "CA-ARCH-TILE", 4, 14),
      ["KT.04.CuaDi"] = ("Công tác Cửa đi & Phụ kiện (Doors & Hardware)", "RES-DOOR (Cửa gỗ công nghiệp & Khóa)", "CA-ARCH-DOOR", 3, 8),
      ["KT.05.CuaSo"] = ("Công tác Cửa sổ & Vách kính (Windows & Glazing)", "RES-WINDOW (Nhôm Xingfa & Kính an toàn)", "CA-ARCH-WINDOW", 3, 10),
      ["KT.06.Tran"] = ("Công tác Trần thạch cao (Gypsum Ceilings)", "RES-CEILING (Khung xương Vĩnh Tường & Thạch cao)", "CA-ARCH-CEILING", 3, 10),
      ["KT.07.SonBa"] = ("Công tác Sơn bả & Hoàn thiện (Painting & Finishes)", "RES-PAINT (Sơn lót & Sơn phủ Dulux)", "CA-ARCH-PAINT", 3, 8),
      ["KT.08.ChongTham"] = ("Công tác Chống thấm (Waterproofing)", "RES-WATERPROOF (Màng khò & SikaTop Seal)", "CA-ARCH-WP", 3, 6),
      ["KT.09.LanCan"] = ("Công tác Lan can & Tay vịn (Railings)", "RES-RAILING (Inox 304 & Kính cường lực)", "CA-ARCH-METAL", 2, 5),
      ["KT.10.Mai"] = ("Công tác Mái & Thoát nước (Roofing & Drainage)", "RES-ROOF (Tôn cách nhiệt PU & Sê nô)", "CA-ARCH-ROOF", 3, 7)
    };

  private static string RemoveDiacritics(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return text ?? string.Empty;
    var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
    var stringBuilder = new StringBuilder(normalizedString.Length);
    foreach (var c in normalizedString)
    {
      var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
      if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
      {
        stringBuilder.Append(c);
      }
    }
    return stringBuilder
      .ToString()
      .Normalize(System.Text.NormalizationForm.FormC)
      .Replace("đ", "d")
      .Replace("Đ", "D");
  }

  private static int GetCount(Dictionary<string, int> dict, string key)
  {
    return dict.TryGetValue(key, out var val) ? val : 0;
  }

  public static List<P6WbsSummaryItem> BuildWbsSummaries(Document doc, bool includeLinks = false, IEnumerable<string>? selectedLinkTitles = null)
  {
    var boqItems = RevitBoQExtractor.ExtractBoQ(doc, includeLinks, selectedLinkTitles);
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

    // 1. Phân hệ Kết cấu (WBS-KC)
    foreach (var kvp in ComponentDefinitions)
    {
      var compCode = kvp.Key;
      var def = kvp.Value;
      var elemCount = GetCount(countByCode, compCode);

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
        Discipline = "KẾT CẤU",
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

    // 2. Phân hệ Kiến trúc (WBS-KT) - khi bật includeLinks hoặc có cấu kiện kiến trúc
    if (includeLinks || countByCode.Keys.Any(k => k.StartsWith("KT.", StringComparison.OrdinalIgnoreCase)))
    {
      foreach (var kvp in ArchComponentDefinitions)
      {
        var compCode = kvp.Key;
        var def = kvp.Value;
        var elemCount = GetCount(countByCode, compCode);

        if (elemCount == 0)
        {
          if (compCode == "KT.02.TratBa" || compCode == "KT.07.SonBa")
            elemCount = GetCount(countByCode, "KT.01.TuongXay");
          else if (compCode == "KT.08.ChongTham")
            elemCount = Math.Max(1, GetCount(countByCode, "KT.03.OpLat") / 2);
        }

        result.Add(new P6WbsSummaryItem
        {
          Discipline = "KIẾN TRÚC",
          ComponentCode = compCode,
          ComponentName = def.Name,
          WbsCode = $"WBS-KT.{compCode.Replace("KT.", "")}",
          ElementCount = elemCount,
          TaskCount = def.DefaultTasks,
          PrimaryResource = def.Resource,
          CostAccount = def.CostAcc,
          DurationDays = def.Duration
        });
      }
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
    var now = DateTime.Now;
    var dateOnly = now.ToString("yyyy-MM-dd");
    var nowStr = now.ToString("yyyy-MM-dd HH:mm");
    var startStr = now.ToString("yyyy-MM-dd 08:00");
    var finishStr = now.AddMonths(6).ToString("yyyy-MM-dd 17:00");

    var sb = new StringBuilder();

    // 1. Header chuẩn Oracle Primavera P6 (khớp 100% định dạng xuất từ P6 Professional)
    sb.AppendLine($"ERMHDR\t25.12\t{dateOnly}\tProject\tADMIN\tkaka\tdbxDatabaseNoName\tProject Management\tUSD");

    // 2. Table CURRTYPE
    sb.AppendLine("%T\tCURRTYPE");
    sb.AppendLine("%F\tcurr_id\tdecimal_digit_cnt\tcurr_symbol\tdecimal_symbol\tdigit_group_symbol\tpos_curr_fmt_type\tneg_curr_fmt_type\tcurr_type\tcurr_short_name\tgroup_digit_cnt\tbase_exch_rate");
    sb.AppendLine("%R\t1\t2\t$\t.\t,\t#1.1\t(#1.1)\tUS Dollar\tUSD\t3\t1");

    // 3. Table OBS
    sb.AppendLine("%T\tOBS");
    sb.AppendLine("%F\tobs_id\tparent_obs_id\tguid\tseq_num\tobs_name\tobs_descr");
    sb.AppendLine("%R\t540\t\t\t0\tEnterprise\t<html><head></head><body bgcolor=\"#ffffff\">Enterprise</body></html>");

    // 4. Table CALENDAR (Lịch làm việc chuẩn 8h/ngày, 5 ngày/tuần)
    sb.AppendLine("%T\tCALENDAR");
    sb.AppendLine("%F\tclndr_id\tdefault_flag\tclndr_name\tproj_id\tbase_clndr_id\tlast_chng_date\tclndr_type\tday_hr_cnt\tweek_hr_cnt\tmonth_hr_cnt\tyear_hr_cnt\trsrc_private\tclndr_data");
    sb.AppendLine($"%R\t178\tY\tCorporate - Standard Full Time\t\t\t2026-01-01 08:00\tCA_Base\t8.0\t40.0\t172.0\t2000.0\tN\t(0||CalendarData()((0||DaysOfWeek()((0||1()())(0||2()((0||0(f|17:00|s|08:00)())))(0||3()((0||0(f|17:00|s|08:00)())))(0||4()((0||0(f|17:00|s|08:00)())))(0||5()((0||0(f|17:00|s|08:00)())))(0||6()((0||0(f|17:00|s|08:00)())))(0||7()())))))");

    // 5. Table PROJECT (có đầy đủ project_flag=Y và export_flag=Y để P6 hiện ngay trong Import Options)
    sb.AppendLine("%T\tPROJECT");
    sb.AppendLine("%F\tproj_id\tfy_start_month_num\trsrc_self_add_flag\tallow_complete_flag\trsrc_multi_assign_flag\tcheckout_flag\tproject_flag\tstep_complete_flag\tcost_qty_recalc_flag\tbatch_sum_flag\tname_sep_char\tdef_complete_pct_type\tproj_short_name\tacct_id\torig_proj_id\tsource_proj_id\tbase_type_id\tclndr_id\tsum_base_proj_id\ttask_code_base\ttask_code_step\tpriority_num\twbs_max_sum_level\tstrgy_priority_num\tlast_checksum\tcritical_drtn_hr_cnt\tdef_cost_per_qty\tlast_recalc_date\tplan_start_date\tplan_end_date\tscd_end_date\tadd_date\tlast_tasksum_date\tfcst_start_date\tdef_duration_type\ttask_code_prefix\tguid\tdef_qty_type\tadd_by_name\tweb_local_root_path\tproj_url\tdef_rate_type\tadd_act_remain_flag\tact_this_per_link_flag\tdef_task_type\tact_pct_link_flag\tcritical_path_type\ttask_code_prefix_flag\tdef_rollup_dates_flag\tuse_project_baseline_flag\trem_target_link_flag\treset_planned_flag\tallow_neg_act_flag\tsum_assign_level\tlast_fin_dates_id\tfintmpl_id\tlast_baseline_update_date\tcr_external_key\tapply_actuals_date\tlocation_id\tlast_schedule_date\tloaded_scope_level\texport_flag\tnew_fin_dates_id\tbaselines_to_export\tbaseline_names_to_export\tnext_data_date\tbase_proj_id\tbase_proj_id1\tbase_proj_id2\tclose_period_flag\tsum_refresh_date\ttrsrcsum_loaded\tsumtask_loaded");
    sb.AppendLine($"%R\t1\t1\tY\tY\tY\tN\tY\tY\tY\tY\t.\tCP_Drtn\tPRJ-KAKA-BIM\t\t\t\t\t178\t\t1000\t10\t10\t0\t100\t\t0\t100.0\t{nowStr}\t{startStr}\t{finishStr}\t{finishStr}\t{nowStr}\t\t\tDT_FixedDUR2\tACT\tKakaBimPrjGuid_{dateOnly}\tQT_Hour\tadmin\t\t\tCOST_PER_QTY\tN\tY\tTT_Task\tN\tCT_TotFloat\tY\tY\tY\tY\tN\tN\tSL_Taskrsrc\t\t\t\t\t\t\t{nowStr}\t7\tY\t\t\t\t\t\t\t\tN\t{nowStr}\t\t");

    // 6. Table SCHEDOPTIONS
    sb.AppendLine("%T\tSCHEDOPTIONS");
    sb.AppendLine("%F\tschedoptions_id\tproj_id\tsched_outer_depend_type\tsched_open_critical_flag\tsched_lag_early_start_flag\tsched_retained_logic\tsched_setplantoforecast\tsched_float_type\tsched_calendar_on_relationship_lag\tsched_use_expect_end_flag\tsched_progress_override\tlevel_float_thrs_cnt\tlevel_outer_assign_flag\tlevel_outer_assign_priority\tlevel_over_alloc_pct\tlevel_within_float_flag\tlevel_keep_sched_date_flag\tlevel_all_rsrc_flag\tsched_use_project_end_date_for_float\tenable_multiple_longest_path_calc\tlimit_multiple_longest_path_calc\tmax_multiple_longest_path\tuse_total_float_multiple_longest_paths\tkey_activity_for_multiple_longest_paths\tLevelPriorityList");
    sb.AppendLine("%R\t1\t1\tSD_Both\tN\tY\tY\tN\tFT_FF\trcal_Predecessor\tY\tN\t0\tN\t5\t25\tN\tY\tY\tY\tN\tY\t10\tY\t\tpriority_type,ASC_BY_FIELD/ASC");

    // 7. Table PROJWBS
    sb.AppendLine("%T\tPROJWBS");
    sb.AppendLine("%F\twbs_id\tproj_id\tobs_id\tseq_num\test_wt\tproj_node_flag\tsum_data_flag\tstatus_code\twbs_short_name\twbs_name\tphase_id\tparent_wbs_id\tev_user_pct\tev_etc_user_value\torig_cost\tindep_remain_total_cost\tann_dscnt_rate_pct\tdscnt_period_type\tindep_remain_work_qty\tanticip_start_date\tanticip_end_date\tev_compute_type\tev_etc_compute_type\tguid\ttmpl_guid\tplan_open_state");
    var cleanProjName = RemoveDiacritics(projectName);

    // Root WBS: proj_node_flag=Y, mã trùng proj_short_name
    sb.AppendLine($"%R\t100\t1\t540\t1\t1\tY\tY\tWS_Open\tPRJ-KAKA-BIM\t{cleanProjName} (Project Root)\t\t\t\t\t0.0\t0.0\t\t\t\t\t\t\t\tRootWbsGuid_{dateOnly}\t\t");
    // Level 1: WBS-KC
    sb.AppendLine($"%R\t110\t1\t540\t2\t1\tN\tY\tWS_Open\tWBS-KC\tPhan He Ket Cau Kaka (Structural Works)\t\t100\t\t\t0.0\t0.0\t\t\t\t\t\t\t\tKcWbsGuid_{dateOnly}\t\t");

    bool hasArch = summaries.Any(x => x.Discipline == "KIẾN TRÚC");
    if (hasArch)
    {
      sb.AppendLine($"%R\t115\t1\t540\t3\t1\tN\tY\tWS_Open\tWBS-KT\tPhan He Kien Truc Kaka (Architectural Works)\t\t100\t\t\t0.0\t0.0\t\t\t\t\t\t\t\tKtWbsGuid_{dateOnly}\t\t");
    }

    int wbsIdCounter = 120;
    var wbsMap = new Dictionary<string, int>();

    foreach (var item in summaries)
    {
      wbsIdCounter++;
      wbsMap[item.ComponentCode] = wbsIdCounter;
      var cleanCompName = RemoveDiacritics(item.ComponentName);
      int parentId = item.Discipline == "KIẾN TRÚC" ? 115 : 110;
      sb.AppendLine($"%R\t{wbsIdCounter}\t1\t540\t{wbsIdCounter - 100}\t1\tN\tY\tWS_Open\t{item.WbsCode}\t{item.ComponentCode} - {cleanCompName}\t\t{parentId}\t\t\t0.0\t0.0\t\t\t\t\t\t\t\tWbsGuid_{wbsIdCounter}\t\t");
    }

    // 8. Table TASK
    sb.AppendLine("%T\tTASK");
    sb.AppendLine("%F\ttask_id\tproj_id\twbs_id\tclndr_id\tphys_complete_pct\trev_fdbk_flag\test_wt\tlock_plan_flag\tauto_compute_act_flag\tcomplete_pct_type\ttask_type\tduration_type\tstatus_code\ttask_code\ttask_name\trsrc_id\ttotal_float_hr_cnt\tfree_float_hr_cnt\tremain_drtn_hr_cnt\tact_work_qty\tremain_work_qty\ttarget_work_qty\ttarget_drtn_hr_cnt\ttarget_equip_qty\tact_equip_qty\tremain_equip_qty\tcstr_date\tact_start_date\tact_end_date\tlate_start_date\tlate_end_date\texpect_end_date\tearly_start_date\tearly_end_date\trestart_date\treend_date\ttarget_start_date\ttarget_end_date\trem_late_start_date\trem_late_end_date\tcstr_type\tpriority_type\tsuspend_date\tresume_date\tfloat_path\tfloat_path_order\tguid\ttmpl_guid\tcstr_date2\tcstr_type2\tdriving_path_flag\tact_this_per_work_qty\tact_this_per_equip_qty\texternal_early_start_date\texternal_late_end_date\tcreate_date\tupdate_date\tcreate_user\tupdate_user\tlocation_id\tcrt_path_num");

    int actIdCounter = 1000;
    var taskList = new List<int>();

    foreach (var item in summaries)
    {
      int currentWbsId = wbsMap[item.ComponentCode];
      var compShort = item.ComponentCode.Replace(".", "").Replace("KT", "");
      var cleanCompName = RemoveDiacritics(item.ComponentName);

      List<(string NameSuffix, int DurationDays)> taskTypes;
      string actCodePrefix = item.Discipline == "KIẾN TRÚC" ? "ACT-KT" : "ACT-KC";

      if (item.Discipline == "KIẾN TRÚC")
      {
        taskTypes = new List<(string NameSuffix, int DurationDays)>
        {
          ("Chuan bi vat tu & dinh vi tim truc", Math.Max(2, (int)(item.DurationDays * 0.25))),
          ("Thi cong lap dung & hoan thien", Math.Max(3, (int)(item.DurationDays * 0.50))),
          ("Nghiem thu chat luong & bao ve be mat", Math.Max(1, (int)(item.DurationDays * 0.25)))
        };
      }
      else
      {
        taskTypes = new List<(string NameSuffix, int DurationDays)>
        {
          ("Thi cong cot thep & dem lot", Math.Max(2, (int)(item.DurationDays * 0.35))),
          ("Lap dung van khuon & gong giang", Math.Max(2, (int)(item.DurationDays * 0.25))),
          ("Do be tong & nghiem thu", Math.Max(1, (int)(item.DurationDays * 0.2))),
          ("Bao duong & hoan thien be mat", Math.Max(1, (int)(item.DurationDays * 0.2)))
        };
      }

      foreach (var t in taskTypes)
      {
        actIdCounter += 10;
        taskList.Add(actIdCounter);
        var actCode = $"{actCodePrefix}-{compShort}-{actIdCounter}";
        var actName = $"{cleanCompName} - {t.NameSuffix}";
        var durationHours = t.DurationDays * 8;

        sb.AppendLine($"%R\t{actIdCounter}\t1\t{currentWbsId}\t178\t0\tN\t1\tN\tY\tCP_Drtn\tTT_Task\tDT_FixedDUR2\tTK_NotStart\t{actCode}\t{actName}\t\t0\t0\t{durationHours}\t\t{durationHours}\t{durationHours}\t{durationHours}\t0\t\t\t\t\t\t{startStr}\t{finishStr}\t\t{startStr}\t{finishStr}\t{startStr}\t{finishStr}\t{startStr}\t{finishStr}\t{startStr}\t{finishStr}\t\t\t\t\t\t\tTaskGuid_{actIdCounter}\t\t\t\tY\t\t\t\t\t{nowStr}\t{nowStr}\tadmin\tadmin\t\t");
      }
    }

    // 9. Table TASKPRED (Mạng logic CPM Finish-to-Start nối tiếp giữa các công tác)
    sb.AppendLine("%T\tTASKPRED");
    sb.AppendLine("%F\ttask_pred_id\ttask_id\tpred_task_id\tproj_id\tpred_proj_id\tpred_type\tlag_hr_cnt\tcomments\tfloat_path\taref\tarls");

    int predIdCounter = 1;
    for (int i = 1; i < taskList.Count; i++)
    {
      predIdCounter++;
      sb.AppendLine($"%R\t{predIdCounter}\t{taskList[i]}\t{taskList[i - 1]}\t1\t1\tPR_FS\t0\t\t\t\t");
    }

    // Ghi file với UTF-8 không BOM (NO BOM) để P6 không bị chặn tại màn hình chọn file
    var utf8WithoutBom = new UTF8Encoding(false);
    File.WriteAllText(xerPath, sb.ToString(), utf8WithoutBom);
  }

  public static bool SyncToP6Sqlite(
    string dbPath,
    string projCode,
    string projTitle,
    List<P6WbsSummaryItem> summaries,
    out string errorMessage)
  {
    errorMessage = string.Empty;
    string? tempPy = null;
    try
    {
      if (!File.Exists(dbPath))
      {
        errorMessage = $"Không tìm thấy CSDL Primavera P6 SQLite tại:\n{dbPath}";
        return false;
      }

      var sbPy = new StringBuilder();
      sbPy.AppendLine("import sys, os, sqlite3, datetime");
      sbPy.AppendLine($"db_path = r\"{dbPath}\"");
      sbPy.AppendLine($"proj_code = \"{projCode.Replace("\"", "\\\"")}\"");
      sbPy.AppendLine($"proj_title = \"{projTitle.Replace("\"", "\\\"")}\"");
      sbPy.AppendLine("try:");
      sbPy.AppendLine("    conn = sqlite3.connect(db_path)");
      sbPy.AppendLine("    cur = conn.cursor()");
      sbPy.AppendLine("    cur.execute('SELECT proj_id FROM project WHERE proj_short_name = ?', (proj_code,))");
      sbPy.AppendLine("    for (pid,) in cur.fetchall():");
      sbPy.AppendLine("        cur.execute('DELETE FROM taskpred WHERE proj_id = ?', (pid,))");
      sbPy.AppendLine("        cur.execute('DELETE FROM task WHERE proj_id = ?', (pid,))");
      sbPy.AppendLine("        cur.execute('DELETE FROM projwbs WHERE proj_id = ?', (pid,))");
      sbPy.AppendLine("        cur.execute('DELETE FROM project WHERE proj_id = ?', (pid,))");
      sbPy.AppendLine("    cur.execute('SELECT MAX(proj_id) FROM project')");
      sbPy.AppendLine("    new_proj_id = (cur.fetchone()[0] or 100) + 1");
      sbPy.AppendLine("    cur.execute('SELECT MAX(wbs_id) FROM projwbs')");
      sbPy.AppendLine("    wbs_counter = (cur.fetchone()[0] or 1000) + 1");
      sbPy.AppendLine("    cur.execute('SELECT MAX(task_id) FROM task')");
      sbPy.AppendLine("    task_counter = (cur.fetchone()[0] or 10000) + 1");
      sbPy.AppendLine("    cur.execute('SELECT MAX(task_pred_id) FROM taskpred')");
      sbPy.AppendLine("    pred_counter = (cur.fetchone()[0] or 100000) + 1");
      sbPy.AppendLine("    now_str = datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')");
      sbPy.AppendLine("    cur.execute('''");
      sbPy.AppendLine("        INSERT INTO project (");
      sbPy.AppendLine("            proj_id, proj_short_name, project_flag,");
      sbPy.AppendLine("            clndr_id, def_complete_pct_type, plan_start_date, plan_end_date,");
      sbPy.AppendLine("            scd_end_date, add_date, last_recalc_date, def_duration_type,");
      sbPy.AppendLine("            def_qty_type, def_rate_type, def_task_type, add_by_name,");
      sbPy.AppendLine("            create_date, create_user, update_date, update_user");
      sbPy.AppendLine("        ) VALUES (");
      sbPy.AppendLine("            ?, ?, 'Y',");
      sbPy.AppendLine("            178, 'CP_Drtn', '2026-10-12 08:00:00', '2027-04-15 17:00:00',");
      sbPy.AppendLine("            '2027-04-15 17:00:00', ?, ?, 'DT_FixedDUR2',");
      sbPy.AppendLine("            'QT_Hour', 'COST_PER_QTY', 'TT_Task', 'admin',");
      sbPy.AppendLine("            ?, 'admin', ?, 'admin'");
      sbPy.AppendLine("        )");
      sbPy.AppendLine("    ''', (new_proj_id, proj_code, now_str, now_str, now_str, now_str))");
      sbPy.AppendLine("    root_wbs_id = wbs_counter");
      sbPy.AppendLine("    wbs_counter += 1");
      sbPy.AppendLine("    cur.execute('''");
      sbPy.AppendLine("        INSERT INTO projwbs (");
      sbPy.AppendLine("            wbs_id, proj_id, obs_id, seq_num, est_wt, proj_node_flag, sum_data_flag,");
      sbPy.AppendLine("            status_code, wbs_short_name, wbs_name, parent_wbs_id,");
      sbPy.AppendLine("            create_date, create_user, update_date, update_user");
      sbPy.AppendLine("        ) VALUES (?, ?, 540, 1, 1.0, 'Y', 'Y', 'WS_Open', ?, ?, 3063, ?, 'admin', ?, 'admin')");
      sbPy.AppendLine("    ''', (root_wbs_id, new_proj_id, proj_code, f'{proj_title} (Project Root)', now_str, now_str))");
      sbPy.AppendLine("    kc_wbs_id = wbs_counter");
      sbPy.AppendLine("    wbs_counter += 1");
      sbPy.AppendLine("    cur.execute('''");
      sbPy.AppendLine("        INSERT INTO projwbs (");
      sbPy.AppendLine("            wbs_id, proj_id, obs_id, seq_num, est_wt, proj_node_flag, sum_data_flag,");
      sbPy.AppendLine("            status_code, wbs_short_name, wbs_name, parent_wbs_id,");
      sbPy.AppendLine("            create_date, create_user, update_date, update_user");
      sbPy.AppendLine("        ) VALUES (?, ?, 540, 2, 1.0, 'N', 'Y', 'WS_Open', 'WBS-KC', 'Phân Hệ Kết Cấu Kaka (Structural Works)', ?, ?, 'admin', ?, 'admin')");
      sbPy.AppendLine("    ''', (kc_wbs_id, new_proj_id, root_wbs_id, now_str, now_str))");

      bool hasArchDb = summaries.Any(x => x.Discipline == "KIẾN TRÚC");
      if (hasArchDb)
      {
        sbPy.AppendLine("    kt_wbs_id = wbs_counter");
        sbPy.AppendLine("    wbs_counter += 1");
        sbPy.AppendLine("    cur.execute('''");
        sbPy.AppendLine("        INSERT INTO projwbs (");
        sbPy.AppendLine("            wbs_id, proj_id, obs_id, seq_num, est_wt, proj_node_flag, sum_data_flag,");
        sbPy.AppendLine("            status_code, wbs_short_name, wbs_name, parent_wbs_id,");
        sbPy.AppendLine("            create_date, create_user, update_date, update_user");
        sbPy.AppendLine("        ) VALUES (?, ?, 540, 3, 1.0, 'N', 'Y', 'WS_Open', 'WBS-KT', 'Phân Hệ Kiến Trúc Kaka (Architectural Works)', ?, ?, 'admin', ?, 'admin')");
        sbPy.AppendLine("    ''', (kt_wbs_id, new_proj_id, root_wbs_id, now_str, now_str))");
      }

      sbPy.AppendLine("    all_task_ids = []");
      sbPy.AppendLine("    seq = 10");

      foreach (var item in summaries)
      {
        var compCode = item.ComponentCode;
        var compShort = item.ComponentCode.Replace(".", "").Replace("KT", "");
        var compName = item.ComponentName.Replace("\"", "\\\"");
        var compDays = item.DurationDays;
        string parentWbsVar = item.Discipline == "KIẾN TRÚC" ? "kt_wbs_id" : "kc_wbs_id";
        string actCodePrefix = item.Discipline == "KIẾN TRÚC" ? "ACT-KT" : "ACT-KC";

        sbPy.AppendLine("    seq += 1");
        sbPy.AppendLine("    comp_wbs_id = wbs_counter");
        sbPy.AppendLine("    wbs_counter += 1");
        sbPy.AppendLine($"    parent_wbs = {parentWbsVar}");
        sbPy.AppendLine($"    cur.execute('''INSERT INTO projwbs (wbs_id, proj_id, obs_id, seq_num, est_wt, proj_node_flag, sum_data_flag, status_code, wbs_short_name, wbs_name, parent_wbs_id, create_date, create_user, update_date, update_user) VALUES (?, ?, 540, ?, 1.0, 'N', 'Y', 'WS_Open', ?, ?, ?, ?, 'admin', ?, 'admin')''', (comp_wbs_id, new_proj_id, seq, '{item.WbsCode}', '{compCode} - {compName}', parent_wbs, now_str, now_str))");

        (string Suffix, double Ratio)[] taskTypes;
        if (item.Discipline == "KIẾN TRÚC")
        {
          taskTypes = new (string Suffix, double Ratio)[]
          {
            ("Chuẩn bị vật tư & định vị tim trục", 0.25),
            ("Thi công lắp dựng & hoàn thiện", 0.50),
            ("Nghiệm thu chất lượng & bảo vệ bề mặt", 0.25)
          };
        }
        else
        {
          taskTypes = new (string Suffix, double Ratio)[]
          {
            ("Thi công cốt thép & đệm lót", 0.35),
            ("Lắp dựng ván khuôn & gông giằng", 0.25),
            ("Đổ bê tông & nghiệm thu", 0.20),
            ("Bảo dưỡng & hoàn thiện bề mặt", 0.20)
          };
        }

        foreach (var t in taskTypes)
        {
          var durDays = Math.Max(1, (int)(compDays * t.Ratio));
          var durHrs = durDays * 8.0;

          sbPy.AppendLine("    current_task_id = task_counter");
          sbPy.AppendLine("    task_counter += 1");
          sbPy.AppendLine("    all_task_ids.append(current_task_id)");
          sbPy.AppendLine($"    act_code = f'{actCodePrefix}-{compShort}-{{current_task_id}}'");
          sbPy.AppendLine($"    act_name = f'{compName} - {t.Suffix}'");
          sbPy.AppendLine($"    t_dur_hrs = {durHrs}");
          sbPy.AppendLine("    cur.execute('''INSERT INTO task (task_id, proj_id, wbs_id, clndr_id, phys_complete_pct, rev_fdbk_flag, est_wt, lock_plan_flag, auto_compute_act_flag, complete_pct_type, task_type, duration_type, status_code, task_code, task_name, total_float_hr_cnt, free_float_hr_cnt, remain_drtn_hr_cnt, remain_work_qty, target_work_qty, target_drtn_hr_cnt, target_equip_qty, late_start_date, late_end_date, early_start_date, early_end_date, restart_date, reend_date, target_start_date, target_end_date, rem_late_start_date, rem_late_end_date, driving_path_flag, create_date, update_date, create_user, update_user) VALUES (?, ?, ?, 178, 0.0, 'N', 1.0, 'N', 'Y', 'CP_Drtn', 'TT_Task', 'DT_FixedDUR2', 'TK_NotStart', ?, ?, 0.0, 0.0, ?, ?, ?, ?, 0.0, '2026-10-12 08:00:00', '2027-04-15 17:00:00', '2026-10-12 08:00:00', '2027-04-15 17:00:00', '2026-10-12 08:00:00', '2027-04-15 17:00:00', '2026-10-12 08:00:00', '2027-04-15 17:00:00', '2026-10-12 08:00:00', '2027-04-15 17:00:00', 'Y', ?, ?, 'admin', 'admin')''', (current_task_id, new_proj_id, comp_wbs_id, act_code, act_name, t_dur_hrs, t_dur_hrs, t_dur_hrs, t_dur_hrs, now_str, now_str))");
        }
      }

      sbPy.AppendLine("    for i in range(1, len(all_task_ids)):");
      sbPy.AppendLine("        pred_counter += 1");
      sbPy.AppendLine("        cur.execute('''INSERT INTO taskpred (task_pred_id, task_id, pred_task_id, proj_id, pred_proj_id, pred_type, lag_hr_cnt, create_date, create_user, update_date, update_user) VALUES (?, ?, ?, ?, ?, 'PR_FS', 0.0, ?, 'admin', ?, 'admin')''', (pred_counter, all_task_ids[i], all_task_ids[i-1], new_proj_id, new_proj_id, now_str, now_str))");
      sbPy.AppendLine("    conn.commit()");
      sbPy.AppendLine("    print('SUCCESS')");
      sbPy.AppendLine("except Exception as e:");
      sbPy.AppendLine("    print(f'ERROR: {e}', file=sys.stderr)");
      sbPy.AppendLine("    sys.exit(1)");
      sbPy.AppendLine("finally:");
      sbPy.AppendLine("    conn.close()");

      tempPy = Path.Combine(Path.GetTempPath(), $"kaka_p6_sync_{Guid.NewGuid():N}.py");
      File.WriteAllText(tempPy, sbPy.ToString(), Encoding.UTF8);

      var psi = new ProcessStartInfo
      {
        FileName = "python",
        Arguments = $"\"{tempPy}\"",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
      };

      using var proc = Process.Start(psi);
      if (proc == null)
      {
        errorMessage = "Không thể khởi động tiến trình Python.";
        return false;
      }

      var stdErr = proc.StandardError.ReadToEnd();
      var stdOut = proc.StandardOutput.ReadToEnd();
      proc.WaitForExit(15000);

      if (proc.ExitCode != 0)
      {
        errorMessage = !string.IsNullOrWhiteSpace(stdErr) ? stdErr : stdOut;
        return false;
      }

      return true;
    }
    catch (Exception ex)
    {
      errorMessage = ex.Message;
      return false;
    }
    finally
    {
      if (tempPy != null && File.Exists(tempPy))
      {
        try { File.Delete(tempPy); } catch { }
      }
    }
  }
}

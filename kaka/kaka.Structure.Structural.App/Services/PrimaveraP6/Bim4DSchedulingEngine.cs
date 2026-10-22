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

/// <summary>
/// Động cơ lập tiến độ 4D BIM tự động (Dynamic 4D Scheduling & CPM Engine).
/// Tự động bóc tách khối lượng từ mô hình 3D, tính toán thời lượng (Duration) theo định mức năng suất,
/// thiết lập mạng quan hệ logic CPM (Finish-to-Start) và xuất ra Primavera P6 XER hoàn chỉnh.
/// </summary>
public static class Bim4DSchedulingEngine
{
  public sealed class ProductivityNorm
  {
    public string Unit { get; set; } = "m3";
    public double RatePerDay { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string CostAccount { get; set; } = string.Empty;
  }

  // Bảng định mức năng suất ca máy / tổ đội chuẩn tổng thầu (GLC / Standard Benchmark)
  public static readonly Dictionary<string, ProductivityNorm> StandardNorms =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["01.Coc"] = new ProductivityNorm { Unit = "cọc", RatePerDay = 12, ResourceName = "RES-PILE (Robot ép & Cọc)", CostAccount = "CA-PILE-00" },
      ["02.Mong"] = new ProductivityNorm { Unit = "m3", RatePerDay = 35, ResourceName = "RES-CONC (Bê tông B25 & Thép móng)", CostAccount = "CA-CONC-01" },
      ["03.DaKieng"] = new ProductivityNorm { Unit = "m3", RatePerDay = 20, ResourceName = "RES-FORMWORK (Ván phủ phim & Thép)", CostAccount = "CA-FORM-03" },
      ["04.Nen"] = new ProductivityNorm { Unit = "m2", RatePerDay = 150, ResourceName = "RES-CONC (Nền xưởng & Màng PVC)", CostAccount = "CA-CONC-01" },
      ["05.Cot"] = new ProductivityNorm { Unit = "m3", RatePerDay = 18, ResourceName = "RES-CONC (Bê tông cột & Thép)", CostAccount = "CA-STEEL-02" },
      ["06.Vach"] = new ProductivityNorm { Unit = "m3", RatePerDay = 22, ResourceName = "RES-CONC (Vách cứng & Ván khuôn vách)", CostAccount = "CA-FORM-03" },
      ["07.Dam"] = new ProductivityNorm { Unit = "m3", RatePerDay = 28, ResourceName = "RES-CONC (Dầm kết cấu)", CostAccount = "CA-CONC-01" },
      ["08.San"] = new ProductivityNorm { Unit = "m3", RatePerDay = 45, ResourceName = "RES-CONC (Sàn kết cấu & Thép 2 lớp)", CostAccount = "CA-CONC-01" },
      ["09.CauThang"] = new ProductivityNorm { Unit = "bộ", RatePerDay = 1, ResourceName = "RES-LABOR (Tổ đội thi công cầu thang)", CostAccount = "CA-DIRECT" },
      ["10.TamCap"] = new ProductivityNorm { Unit = "m3", RatePerDay = 10, ResourceName = "RES-CONC (Tam cấp)", CostAccount = "CA-DIRECT" },
      ["11.Ramp"] = new ProductivityNorm { Unit = "m3", RatePerDay = 15, ResourceName = "RES-CONC (Ramp dốc)", CostAccount = "CA-CONC-01" },
      ["12.Pit"] = new ProductivityNorm { Unit = "m3", RatePerDay = 12, ResourceName = "RES-CONC (Hố Pit)", CostAccount = "CA-CONC-01" },
      ["14.CauKienPhu"] = new ProductivityNorm { Unit = "m3", RatePerDay = 8, ResourceName = "RES-CONC (Cấu kiện phụ)", CostAccount = "CA-DIRECT" },

      // Kiến trúc
      ["KT.01.TuongXay"] = new ProductivityNorm { Unit = "m2", RatePerDay = 25, ResourceName = "RES-BRICK (Gạch tuynel & Vữa M75)", CostAccount = "CA-ARCH-WALL" },
      ["KT.02.TratBa"] = new ProductivityNorm { Unit = "m2", RatePerDay = 35, ResourceName = "RES-PLASTER (Thợ trát & Vữa M75)", CostAccount = "CA-ARCH-FINISH" },
      ["KT.03.OpLat"] = new ProductivityNorm { Unit = "m2", RatePerDay = 22, ResourceName = "RES-TILE (Gạch Granite 600x600)", CostAccount = "CA-ARCH-TILE" },
      ["KT.04.CuaDi"] = new ProductivityNorm { Unit = "bộ", RatePerDay = 6, ResourceName = "RES-DOOR (Cửa đi & Phụ kiện)", CostAccount = "CA-ARCH-DOOR" },
      ["KT.05.CuaSo"] = new ProductivityNorm { Unit = "m2", RatePerDay = 15, ResourceName = "RES-WINDOW (Nhôm kính Xingfa)", CostAccount = "CA-ARCH-WINDOW" },
      ["KT.06.Tran"] = new ProductivityNorm { Unit = "m2", RatePerDay = 40, ResourceName = "RES-CEILING (Trần thạch cao)", CostAccount = "CA-ARCH-CEILING" },
      ["KT.07.SonBa"] = new ProductivityNorm { Unit = "m2", RatePerDay = 60, ResourceName = "RES-PAINT (Sơn nước nội ngoại thất)", CostAccount = "CA-ARCH-PAINT" },
      ["KT.08.ChongTham"] = new ProductivityNorm { Unit = "m2", RatePerDay = 50, ResourceName = "RES-WATERPROOF (Màng chống thấm)", CostAccount = "CA-ARCH-WP" },
      ["KT.09.LanCan"] = new ProductivityNorm { Unit = "m", RatePerDay = 15, ResourceName = "RES-RAILING (Lan can Inox)", CostAccount = "CA-ARCH-METAL" },
      ["KT.10.Mai"] = new ProductivityNorm { Unit = "m2", RatePerDay = 80, ResourceName = "RES-ROOF (Mái tôn PU cách nhiệt)", CostAccount = "CA-ARCH-ROOF" },

      // MEP
      ["MEP.01.ChuaChay"] = new ProductivityNorm { Unit = "m", RatePerDay = 30, ResourceName = "RES-MEP (Đội cơ điện PCCC)", CostAccount = "CA-MEP-FIRE" },
      ["MEP.02.CapThoatNuoc"] = new ProductivityNorm { Unit = "m", RatePerDay = 25, ResourceName = "RES-MEP (Đội cấp thoát nước)", CostAccount = "CA-MEP-PLUMB" },
      ["MEP.03.DienChieuSang"] = new ProductivityNorm { Unit = "m", RatePerDay = 40, ResourceName = "RES-MEP (Đội điện chiếu sáng)", CostAccount = "CA-MEP-ELEC" },
      ["MEP.04.HVAC"] = new ProductivityNorm { Unit = "m", RatePerDay = 20, ResourceName = "RES-MEP (Đội thông gió & ĐHKK)", CostAccount = "CA-MEP-HVAC" }
    };

  /// <summary>
  /// Trích xuất toàn bộ công tác 4D từ mô hình Revit và tính toán Duration + CPM Logic.
  /// </summary>
  public static List<Bim4DTaskItem> Generate4DTasks(Document doc, bool includeLinks = true)
  {
    var boqItems = RevitBoQExtractor.ExtractBoQ(doc, includeLinks);
    var countByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    var volumeByCode = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

    foreach (var b in boqItems)
    {
      if (string.IsNullOrEmpty(b.CategoryCode)) continue;
      countByCode[b.CategoryCode] = countByCode.TryGetValue(b.CategoryCode, out var c) ? c + 1 : 1;
      volumeByCode[b.CategoryCode] = volumeByCode.TryGetValue(b.CategoryCode, out var v) ? v + b.VolumeM3 : b.VolumeM3;
    }

    var tasks = new List<Bim4DTaskItem>();
    int taskCounter = 1000;
    var baseDate = DateTime.Today.AddDays(7); // Bắt đầu tuần tới
    bool hasPiles = countByCode.TryGetValue("01.Coc", out var pileCnt) && pileCnt > 0;

    // Danh sách các mã công tác tuần tự theo biện pháp thi công
    // Nếu mô hình không có cọc (móng nông, móng đơn/băng), MÓNG là công tác số 1 xuất phát ngay từ baseDate
    var sequenceDefinitions = new (string Code, string Discipline, string WbsCode, string Name, string SearchSet, string? PredecessorCode, int LagDays)[]
    {
      ("01.Coc", "KẾT CẤU", "WBS-KC.01.Coc", "Thi công Cọc & Ép cọc thí nghiệm", "KC_01_Coc_Piles", null, 0),
      ("02.Mong", "KẾT CẤU", "WBS-KC.02.Mong", "Thi công Móng & Bê tông móng", "KC_02_Mong_Foundations", hasPiles ? "01.Coc" : null, hasPiles ? 1 : 0),
      ("03.DaKieng", "KẾT CẤU", "WBS-KC.03.DaKieng", "Thi công Đà kiềng & Giằng móng", "KC_03_DaKieng_TieBeams", "02.Mong", 1),
      ("12.Pit", "KẾT CẤU", "WBS-KC.12.Pit", "Thi công Hố Pit thang máy", "KC_12_Pit_ElevatorPits", "02.Mong", 0),
      ("04.Nen", "KẾT CẤU", "WBS-KC.04.Nen", "Thi công Nền hạ tầng & Slab on Grade", "KC_04_Nen_SlabOnGrade", "03.DaKieng", 1),
      ("05.Cot", "KẾT CẤU", "WBS-KC.05.Cot", "Thi công Cột kết cấu", "KC_05_Cot_Columns", "04.Nen", 1),
      ("06.Vach", "KẾT CẤU", "WBS-KC.06.Vach", "Thi công Vách cứng & Vách thang máy", "KC_06_Vach_ShearWalls", "05.Cot", 0),
      ("07.Dam", "KẾT CẤU", "WBS-KC.07.Dam", "Thi công Dầm kết cấu", "KC_07_Dam_FramingBeams", "05.Cot", 1),
      ("08.San", "KẾT CẤU", "WBS-KC.08.San", "Thi công Sàn kết cấu & Đổ bê tông sàn", "KC_08_San_StructuralSlabs", "07.Dam", 0),
      ("09.CauThang", "KẾT CẤU", "WBS-KC.09.CauThang", "Thi công Cầu thang bộ", "KC_09_CauThang_Stairs", "08.San", 1),
      ("11.Ramp", "KẾT CẤU", "WBS-KC.11.Ramp", "Thi công Ramp dốc", "KC_11_Ramp_Doc", "08.San", 1),
      ("10.TamCap", "KẾT CẤU", "WBS-KC.10.TamCap", "Thi công Tam cấp lối vào", "KC_10_TamCap_Steps", "04.Nen", 2),
      ("14.CauKienPhu", "KẾT CẤU", "WBS-KC.14.CauKienPhu", "Thi công Cấu kiện phụ & Hoàn thiện KC", "KC_14_CauKienPhu_Secondary", "08.San", 2),

      // Kiến trúc
      ("KT.01.TuongXay", "KIẾN TRÚC", "WBS-KT.01.TuongXay", "Thi công Tường xây gạch bao & ngăn phòng", "KT_01_TuongXay_BrickWalls", "08.San", 4), // Sau tháo dỡ ván khuôn sàn
      ("KT.08.ChongTham", "KIẾN TRÚC", "WBS-KT.08.ChongTham", "Thi công Chống thấm WC & Ban công", "KT_08_ChongTham_Waterproofing", "KT.01.TuongXay", 1),
      ("KT.02.TratBa", "KIẾN TRÚC", "WBS-KT.02.TratBa", "Thi công Trát tường trong & ngoài", "KT_02_TratBa_Plastering", "KT.01.TuongXay", 2),
      ("KT.06.Tran", "KIẾN TRÚC", "WBS-KT.06.Tran", "Lắp dựng Trần thạch cao", "KT_06_Tran_Ceilings", "KT.02.TratBa", 2),
      ("KT.03.OpLat", "KIẾN TRÚC", "WBS-KT.03.OpLat", "Thi công Ốp lát gạch nền & tường", "KT_03_OpLat_Tiling", "KT.08.ChongTham", 1),
      ("KT.05.CuaSo", "KIẾN TRÚC", "WBS-KT.05.CuaSo", "Lắp đặt Cửa sổ & Vách kính", "KT_05_CuaSo_Windows", "KT.02.TratBa", 1),
      ("KT.04.CuaDi", "KIẾN TRÚC", "WBS-KT.04.CuaDi", "Lắp đặt Cửa đi & Phụ kiện", "KT_04_CuaDi_Doors", "KT.03.OpLat", 1),
      ("KT.07.SonBa", "KIẾN TRÚC", "WBS-KT.07.SonBa", "Thi công Sơn bả & Hoàn thiện", "KT_07_SonBa_Painting", "KT.06.Tran", 2),
      ("KT.10.Mai", "KIẾN TRÚC", "WBS-KT.10.Mai", "Lắp dựng Mái tôn & Thoát nước mái", "KT_10_Mai_Roofing", "08.San", 5),
      ("KT.09.LanCan", "KIẾN TRÚC", "WBS-KT.09.LanCan", "Lắp đặt Lan can & Tay vịn", "KT_09_LanCan_Railings", "KT.07.SonBa", 1),

      // MEP
      ("MEP.01.ChuaChay", "CƠ ĐIỆN", "WBS-MEP.01.ChuaChay", "Lắp đặt Hệ thống PCCC & Chữa cháy", "MEP_01_ChuaChay_FireFighting", "08.San", 3),
      ("MEP.02.CapThoatNuoc", "CƠ ĐIỆN", "WBS-MEP.02.CapThoatNuoc", "Lắp đặt Hệ thống Cấp thoát nước", "MEP_02_CapThoatNuoc_Plumbing", "KT.01.TuongXay", 1),
      ("MEP.03.DienChieuSang", "CƠ ĐIỆN", "WBS-MEP.03.DienChieuSang", "Lắp đặt Hệ thống Điện chiếu sáng", "MEP_03_DienChieuSang_Electrical", "KT.06.Tran", 1),
      ("MEP.04.HVAC", "CƠ ĐIỆN", "WBS-MEP.04.HVAC", "Lắp đặt Hệ thống Thông gió & ĐHKK", "MEP_04_DieuHoaThongGio_HVAC", "08.San", 4)
    };

    var taskMapByCode = new Dictionary<string, Bim4DTaskItem>(StringComparer.OrdinalIgnoreCase);

    foreach (var seq in sequenceDefinitions)
    {
      taskCounter += 10;
      var compCode = seq.Code;
      var elemCount = countByCode.TryGetValue(compCode, out var cnt) ? cnt : 0;
      var vol = volumeByCode.TryGetValue(compCode, out var v) ? v : 0;

      // Ước tính số lượng theo đơn vị định mức
      StandardNorms.TryGetValue(compCode, out var norm);
      norm ??= new ProductivityNorm { Unit = "m3", RatePerDay = 30, ResourceName = "RES-GENERAL", CostAccount = "CA-DIRECT" };

      double qty;
      if (norm.Unit == "m3")
      {
        qty = vol > 0 ? Math.Round(vol, 1) : Math.Max(15, elemCount * 3.5);
      }
      else if (norm.Unit == "m2")
      {
        qty = Math.Max(50, elemCount * 12.0);
      }
      else
      {
        qty = Math.Max(2, elemCount > 0 ? elemCount : 8);
      }

      // Tính thời lượng: Duration = Ceil(Qty / Rate)
      int duration = Math.Max(2, (int)Math.Ceiling(qty / norm.RatePerDay));

      var taskItem = new Bim4DTaskItem
      {
        Id = taskCounter,
        TaskCode = $"ACT-4D-{compCode.Replace(".", "")}-{taskCounter}",
        TaskName = seq.SearchSet, // Khớp 100% với tên Search Set để Auto-Attach tức thì
        WbsCode = seq.WbsCode,
        Discipline = seq.Discipline,
        SearchSetName = seq.SearchSet,
        Quantity = qty,
        Unit = norm.Unit,
        ProductivityRate = norm.RatePerDay,
        DurationDays = duration,
        PrimaryResource = norm.ResourceName,
        CostAccount = norm.CostAccount,
        ElementCount = elemCount,
        TaskType = "Construct"
      };

      taskMapByCode[compCode] = taskItem;
      tasks.Add(taskItem);
    }

    // Thiết lập CPM Dates & Predecessors logic
    foreach (var seq in sequenceDefinitions)
    {
      var current = taskMapByCode[seq.Code];
      DateTime calculatedStart = baseDate;

      if (!string.IsNullOrEmpty(seq.PredecessorCode) && taskMapByCode.TryGetValue(seq.PredecessorCode, out var predTask))
      {
        current.PredecessorCodes.Add(predTask.TaskCode);
        calculatedStart = predTask.EndDate.AddDays(seq.LagDays);
      }

      current.StartDate = calculatedStart;
      current.EndDate = calculatedStart.AddDays(current.DurationDays);
    }

    // Xác định Critical Path (Đường găng móng -> dầm sàn -> trát bả -> hoàn thiện)
    var criticalCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "01.Coc", "02.Mong", "03.DaKieng", "04.Nen", "05.Cot", "07.Dam", "08.San",
      "KT.01.TuongXay", "KT.02.TratBa", "KT.06.Tran", "KT.07.SonBa"
    };

    foreach (var t in tasks)
    {
      var matchingDef = sequenceDefinitions.FirstOrDefault(x => x.SearchSet == t.SearchSetName);
      if (matchingDef.Code != null && criticalCodes.Contains(matchingDef.Code))
      {
        t.IsCritical = true;
        t.TotalFloatDays = 0;
      }
      else
      {
        t.IsCritical = false;
        t.TotalFloatDays = 5;
      }
    }

    return tasks;
  }

  /// <summary>
  /// Xuất file Oracle Primavera P6 XER có sẵn bảng quan hệ logic TASKPRED (CPM Network).
  /// </summary>
  public static void Export4DToP6Xer(string xerPath, string projectName, List<Bim4DTaskItem> tasks)
  {
    var now = DateTime.Now;
    var dateOnly = now.ToString("yyyy-MM-dd");
    var nowStr = now.ToString("yyyy-MM-dd HH:mm");
    var startStr = tasks.Count > 0 ? tasks.Min(x => x.StartDate).ToString("yyyy-MM-dd 08:00") : nowStr;
    var finishStr = tasks.Count > 0 ? tasks.Max(x => x.EndDate).ToString("yyyy-MM-dd 17:00") : now.AddMonths(6).ToString("yyyy-MM-dd 17:00");

    var sb = new StringBuilder();

    // 1. Header
    sb.AppendLine($"ERMHDR\t25.12\t{dateOnly}\tProject\tADMIN\tkaka\tdbxDatabaseNoName\tProject Management\tUSD");

    // 2. CURRTYPE
    sb.AppendLine("%T\tCURRTYPE");
    sb.AppendLine("%F\tcurr_id\tdecimal_digit_cnt\tcurr_symbol\tdecimal_symbol\tdigit_group_symbol\tpos_curr_fmt_type\tneg_curr_fmt_type\tcurr_type\tcurr_short_name\tgroup_digit_cnt\tbase_exch_rate");
    sb.AppendLine("%R\t1\t2\t$\t.\t,\t#1.1\t(#1.1)\tUS Dollar\tUSD\t3\t1");

    // 3. OBS
    sb.AppendLine("%T\tOBS");
    sb.AppendLine("%F\tobs_id\tparent_obs_id\tguid\tseq_num\tobs_name\tobs_descr");
    sb.AppendLine("%R\t540\t\t\t0\tEnterprise\t<html><head></head><body bgcolor=\"#ffffff\">Enterprise</body></html>");

    // 4. CALENDAR
    sb.AppendLine("%T\tCALENDAR");
    sb.AppendLine("%F\tclndr_id\tdefault_flag\tclndr_name\tproj_id\tbase_clndr_id\tlast_chng_date\tclndr_type\tday_hr_cnt\tweek_hr_cnt\tmonth_hr_cnt\tyear_hr_cnt\trsrc_private\tclndr_data");
    sb.AppendLine($"%R\t178\tY\tStandard 6 Days Week\t\t\t2026-01-01 08:00\tCA_Base\t8.0\t48.0\t200.0\t2400.0\tN\t(0||CalendarData()((0||DaysOfWeek()((0||1()())(0||2()((0||0(f|17:00|s|08:00)())))(0||3()((0||0(f|17:00|s|08:00)())))(0||4()((0||0(f|17:00|s|08:00)())))(0||5()((0||0(f|17:00|s|08:00)())))(0||6()((0||0(f|17:00|s|08:00)())))(0||7()((0||0(f|17:00|s|08:00)()))))))");

    // 5. PROJECT
    sb.AppendLine("%T\tPROJECT");
    sb.AppendLine("%F\tproj_id\tfy_start_month_num\trsrc_self_add_flag\tallow_complete_flag\trsrc_multi_assign_flag\tcheckout_flag\tproject_flag\tstep_complete_flag\tcost_qty_recalc_flag\tbatch_sum_flag\tname_sep_char\tdef_complete_pct_type\tproj_short_name\tacct_id\torig_proj_id\tsource_proj_id\tbase_type_id\tclndr_id\tsum_base_proj_id\ttask_code_base\ttask_code_step\tpriority_num\twbs_max_sum_level\tstrgy_priority_num\tlast_checksum\tcritical_drtn_hr_cnt\tdef_cost_per_qty\tlast_recalc_date\tplan_start_date\tplan_end_date\tscd_end_date\tadd_date\tlast_tasksum_date\tfcst_start_date\tdef_duration_type\ttask_code_prefix\tguid\tdef_qty_type\tadd_by_name\tweb_local_root_path\tproj_url\tdef_rate_type\tadd_act_remain_flag\tact_this_per_link_flag\tdef_task_type\tact_pct_link_flag\tcritical_path_type\ttask_code_prefix_flag\tdef_rollup_dates_flag\tuse_project_baseline_flag\trem_target_link_flag\treset_planned_flag\tallow_neg_act_flag\tsum_assign_level\tlast_fin_dates_id\tfintmpl_id\tlast_baseline_update_date\tcr_external_key\tapply_actuals_date\tlocation_id\tlast_schedule_date\tloaded_scope_level\texport_flag\tnew_fin_dates_id\tbaselines_to_export\tbaseline_names_to_export\tnext_data_date\tbase_proj_id\tbase_proj_id1\tbase_proj_id2\tclose_period_flag\tsum_refresh_date\ttrsrcsum_loaded\tsumtask_loaded");
    sb.AppendLine($"%R\t1\t1\tY\tY\tY\tN\tY\tY\tY\tY\t.\tCP_Drtn\tPRJ-4D-KAKA\t\t\t\t\t178\t\t1000\t10\t10\t0\t100\t\t0\t100.0\t{nowStr}\t{startStr}\t{finishStr}\t{finishStr}\t{nowStr}\t\t\tDT_FixedDUR2\tACT\tKaka4DPrjGuid_{dateOnly}\tQT_Hour\tadmin\t\t\tCOST_PER_QTY\tN\tY\tTT_Task\tN\tCT_TotFloat\tY\tY\tY\tY\tN\tN\tSL_Taskrsrc\t\t\t\t\t\t\t{nowStr}\t7\tY\t\t\t\t\t\t\t\tN\t{nowStr}\t\t");

    // 6. SCHEDOPTIONS
    sb.AppendLine("%T\tSCHEDOPTIONS");
    sb.AppendLine("%F\tschedoptions_id\tproj_id\tsched_outer_depend_type\tsched_open_critical_flag\tsched_lag_early_start_flag\tsched_retained_logic\tsched_setplantoforecast\tsched_float_type\tsched_calendar_on_relationship_lag\tsched_use_expect_end_flag\tsched_progress_override\tlevel_float_thrs_cnt\tlevel_outer_assign_flag\tlevel_outer_assign_priority\tlevel_over_alloc_pct\tlevel_within_float_flag\tlevel_keep_sched_date_flag\tlevel_all_rsrc_flag\tsched_use_project_end_date_for_float\tenable_multiple_longest_path_calc\tlimit_multiple_longest_path_calc\tmax_multiple_longest_path\tuse_total_float_multiple_longest_paths\tkey_activity_for_multiple_longest_paths\tLevelPriorityList");
    sb.AppendLine("%R\t1\t1\tSD_Both\tN\tY\tY\tN\tFT_FF\trcal_Predecessor\tY\tN\t0\tN\t5\t25\tN\tY\tY\tY\tN\tY\t10\tY\t\tpriority_type,ASC_BY_FIELD/ASC");

    // 7. PROJWBS
    sb.AppendLine("%T\tPROJWBS");
    sb.AppendLine("%F\twbs_id\tproj_id\tobs_id\tseq_num\test_wt\tproj_node_flag\tsum_data_flag\tstatus_code\twbs_short_name\twbs_name\tphase_id\tparent_wbs_id\tev_user_pct\tev_etc_user_value\torig_cost\tindep_remain_total_cost\tann_dscnt_rate_pct\tdscnt_period_type\tindep_remain_work_qty\tanticip_start_date\tanticip_end_date\tev_compute_type\tev_etc_compute_type\tguid\ttmpl_guid\tplan_open_state");
    
    // Root WBS
    sb.AppendLine($"%R\t100\t1\t540\t1\t1\tY\tY\tWS_Open\tPRJ-4D-KAKA\t{projectName} (4D BIM Master Schedule)\t\t\t\t\t0.0\t0.0\t\t\t\t\t\t\t\tRoot4DWbsGuid\t\t");
    // Disciplines
    sb.AppendLine($"%R\t110\t1\t540\t2\t1\tN\tY\tWS_Open\tWBS-KC\tPhan He Ket Cau (Structural 4D)\t\t100\t\t\t0.0\t0.0\t\t\t\t\t\t\t\tKc4DGuid\t\t");
    sb.AppendLine($"%R\t120\t1\t540\t3\t1\tN\tY\tWS_Open\tWBS-KT\tPhan He Kien Truc (Architectural 4D)\t\t100\t\t\t0.0\t0.0\t\t\t\t\t\t\t\tKt4DGuid\t\t");
    sb.AppendLine($"%R\t130\t1\t540\t4\t1\tN\tY\tWS_Open\tWBS-MEP\tPhan He Co Dien (MEP 4D)\t\t100\t\t\t0.0\t0.0\t\t\t\t\t\t\t\tMep4DGuid\t\t");

    int wbsIdCounter = 200;
    var wbsMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    foreach (var group in tasks.GroupBy(x => (x.WbsCode, x.Discipline)))
    {
      wbsIdCounter += 10;
      var wbsCode = group.Key.WbsCode;
      var disc = group.Key.Discipline;
      int parentId = disc == "KẾT CẤU" ? 110 : (disc == "KIẾN TRÚC" ? 120 : 130);
      var wbsName = group.First().TaskName;

      sb.AppendLine($"%R\t{wbsIdCounter}\t1\t540\t{wbsIdCounter}\t1\tN\tY\tWS_Open\t{wbsCode}\t{wbsName}\t\t{parentId}\t\t\t0.0\t0.0\t\t\t\t\t\t\t\tWbsGuid_{wbsIdCounter}\t\t");
      wbsMap[wbsCode] = wbsIdCounter;
    }

    // 8. TASK
    sb.AppendLine("%T\tTASK");
    sb.AppendLine("%F\ttask_id\tproj_id\twbs_id\tclndr_id\tphys_complete_pct\trev_fxd_cost\tlock_plan_flag\tauto_compute_act_flag\tcomplete_pct_type\ttask_type\tduration_type\tstatus_code\ttask_code\ttask_name\trsrc_id\ttotal_float_hr_cnt\tfree_float_hr_cnt\trem_dur_hr_cnt\topen_dur_hr_cnt\ttarget_drtn_hr_cnt\ttarget_work_qty\ttarget_start_date\ttarget_end_date\trem_work_qty\tremain_drtn_hr_cnt\tact_work_qty\tact_start_date\tact_end_date\trestart_date\treend_date\tguid\tact_this_per_work_qty\tact_this_per_cost");

    var taskIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    foreach (var t in tasks)
    {
      int currentWbsId = wbsMap.TryGetValue(t.WbsCode, out var wid) ? wid : 110;
      int durHours = t.DurationDays * 8;
      var startFormatted = t.StartDate.ToString("yyyy-MM-dd 08:00");
      var endFormatted = t.EndDate.ToString("yyyy-MM-dd 17:00");
      int floatHours = t.IsCritical ? 0 : t.TotalFloatDays * 8;

      sb.AppendLine($"%R\t{t.Id}\t1\t{currentWbsId}\t178\t0\t0.0\tN\tY\tCP_Drtn\tTT_Task\tDT_FixedDUR2\tTK_NotStart\t{t.TaskCode}\t{t.TaskName}\t\t{floatHours}\t{floatHours}\t{durHours}\t{durHours}\t{durHours}\t0.0\t{startFormatted}\t{endFormatted}\t0.0\t{durHours}\t0.0\t\t\t\t\tTaskGuid_{t.Id}\t0.0\t0.0");
      taskIdMap[t.TaskCode] = t.Id;
    }

    // 9. TASKPRED (MẠNG QUAN HỆ CPM PREDECESSOR LINKS)
    sb.AppendLine("%T\tTASKPRED");
    sb.AppendLine("%F\ttask_pred_id\ttask_id\tpred_task_id\tproj_id\tpred_type\tlag_hr_cnt\tcomments");

    int predCounter = 1;
    foreach (var t in tasks)
    {
      foreach (var predCode in t.PredecessorCodes)
      {
        if (taskIdMap.TryGetValue(predCode, out var predId))
        {
          // Quan hệ FS (Finish-to-Start) chuẩn CPM
          sb.AppendLine($"%R\t{predCounter++}\t{t.Id}\t{predId}\t1\tPR_FS\t0.0\tCPM Logic from KAKA 4D Engine");
        }
      }
    }

    File.WriteAllText(xerPath, sb.ToString(), Encoding.UTF8);
  }
}

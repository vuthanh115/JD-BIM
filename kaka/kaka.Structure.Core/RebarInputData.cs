namespace kaka.Structure.Core
{
  public class RebarInputData
  {
    public double ColWidth { get; set; }
    public double ColDepth { get; set; }
    public double Cover { get; set; }
    public double BarDiameter { get; set; }
    public double FloorHeight { get; set; }
    public double LapLength { get; set; }
    
    // --- THÊM BIẾN NÀY ---
    // Khoảng cách từ mặt sàn đến chân thép (Splice distance from column base)
    public double SpliceOffset { get; set; }
        
    public bool IsTopColumn { get; set; }
    public bool IsBottomColumn { get; set; }
    public bool IsStaggered { get; set; }
    public double CrankShift { get; set; } = 50.0;
        
    // --- THÔNG SỐ MÓNG (FOUNDATION) ---
    public bool HasStarterBar { get; set; }
    public double EmbedLength { get; set; } 
    public double FootLength { get; set; } 
    public bool StarterBarOutward { get; set; } = true;
    public bool StarterBarLeftward { get; set; }
    public bool StarterBarRightward { get; set; }
    public bool StarterBarBackward { get; set; }
    public bool StarterBarFrontward { get; set; } 

    // --- [MỚI] THÔNG SỐ NEO ĐỈNH (TOP HOOK) ---
    public bool HasTopHook { get; set; }    // Có bẻ móc neo không?
    public double TopHookLength { get; set; } // Chiều dài móc neo
  }
}
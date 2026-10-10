using System;
using System.Collections.Generic;

namespace kaka.Structure.Core
{
    public static class RebarCalculator
    {
        public static List<Point3D> CalculateSingleBarPath(RebarInputData input, int index, double localX_Ratio, double localY_Ratio)
        {
            var points = new List<Point3D>();

            // 1. Tọa độ chuẩn
            double offset = input.Cover + input.BarDiameter / 2.0;
            double effW = input.ColWidth - 2 * offset;
            double effD = input.ColDepth - 2 * offset;

            double xNom = -effW / 2.0 + (localX_Ratio * effW);
            double yNom = -effD / 2.0 + (localY_Ratio * effD);

            // 2. TÍNH CAO ĐỘ Z (ĐÃ SỬA LỖI LOGIC OFFSET)
            
            // [FIX FINAL 1] Không dùng IsBottomColumn để ép zStart về 0 nữa.
            // Hãy tin tưởng vào giá trị SpliceOffset truyền từ ViewModel.
            // (ViewModel sẽ gửi 0 nếu là móng thật, gửi 100 nếu là cột tầng trên).
            double zStart = input.SpliceOffset; 

            double staggerDist = (input.IsStaggered) ? input.LapLength * 0.5 : 0;
            
            // Nếu có so le -> Dời chân lên
            if (input.IsStaggered && index % 2 != 0)
            {
                zStart += staggerDist;
            }

            // Tính Đỉnh
            double zTop = input.FloorHeight;
            if (!input.IsTopColumn) 
            {
                // Đỉnh thanh chờ tầng dưới kết thúc thấp hơn điểm bắt đầu bẻ uốn của thanh trên 15mm
                // để bán kính uốn cong (bend fillet) của thanh trên không chém vào mặt cắt đầu thanh dưới
                zTop += (input.LapLength - 15.0);
                zTop += input.SpliceOffset; // Đỉnh cũng dời lên theo
                if (input.IsStaggered && index % 2 != 0) zTop += staggerDist;
            }
            else
            {
                zTop -= input.Cover; // Cột mái
            }

            // 3. LOGIC VẼ HÌNH DÁNG
            bool isUpperColumn = !input.IsBottomColumn;

            // --- TRƯỜNG HỢP A: CỘT TẦNG TRÊN CÓ BẺ (CRANK) ---
            if (isUpperColumn)
            {
                double dirX = 0, dirY = 0;
                if (Math.Abs(xNom) > 0.1) dirX = (xNom > 0) ? -1 : 1;
                if (Math.Abs(yNom) > 0.1) dirY = (yNom > 0) ? -1 : 1;
                if (dirX != 0 && dirY != 0) { dirX *= 0.7071; dirY *= 0.7071; }

                double slope = 6.0; 
                double crankH = input.BarDiameter * slope;
                // Khe hở 2mm để hai thanh thép ôm sát song song mà không cấn va chạm 3D
                double offsetIn = input.BarDiameter + 2.0;
                double xFoot = xNom + (dirX * offsetIn);
                double yFoot = yNom + (dirY * offsetIn);
                
                // Chuẩn thiết kế kết cấu: Thanh trên đi thẳng hết đoạn nối chồng LapLength rồi uốn bẻ ra vị trí danh định
                points.Add(new Point3D(xFoot, yFoot, zStart));
                points.Add(new Point3D(xFoot, yFoot, zStart + input.LapLength));
                points.Add(new Point3D(xNom, yNom, zStart + input.LapLength + crankH));
                points.Add(new Point3D(xNom, yNom, zTop));
            }
            // --- TRƯỜNG HỢP B: CỘT MÓNG HOẶC CỘT TRÊN ÉP THẲNG (STRAIGHT) ---
            else
            {
                // Nếu là MÓNG THẬT (Có râu chờ cắm xuống đất)
                if (input.HasStarterBar)
                {
                    double dirX = 0, dirY = 0;
                    double layerZOffset = 0;

                    if (input.StarterBarRightward)
                    {
                        dirX = 1; dirY = 0;
                        // Thanh phía bên trái (xNom < 0) vắt ngang qua thanh bên phải -> nâng lên tầng 2
                        if (xNom < -0.1) layerZOffset = input.BarDiameter + 4.0;
                    }
                    else if (input.StarterBarLeftward)
                    {
                        dirX = -1; dirY = 0;
                        // Thanh phía bên phải (xNom > 0) vắt ngang qua thanh bên trái -> nâng lên tầng 2
                        if (xNom > 0.1) layerZOffset = input.BarDiameter + 4.0;
                    }
                    else if (input.StarterBarBackward)
                    {
                        dirX = 0; dirY = 1;
                        // Thanh phía trước (yNom < 0) vắt qua thanh phía sau -> nâng lên tầng 2
                        if (yNom < -0.1) layerZOffset = input.BarDiameter + 4.0;
                    }
                    else if (input.StarterBarFrontward)
                    {
                        dirX = 0; dirY = -1;
                        // Thanh phía sau (yNom > 0) vắt qua thanh phía trước -> nâng lên tầng 2
                        if (yNom > 0.1) layerZOffset = input.BarDiameter + 4.0;
                    }
                    else
                    {
                        // Mặc định bẻ xòe ra ngoài (Outward) theo chuẩn neo móng
                        bool outward = input.StarterBarOutward;
                        if (Math.Abs(xNom) > 0.1) dirX = outward ? ((xNom > 0) ? 1 : -1) : ((xNom > 0) ? -1 : 1);
                        if (Math.Abs(yNom) > 0.1) dirY = outward ? ((yNom > 0) ? 1 : -1) : ((yNom > 0) ? -1 : 1);
                        if (dirX != 0 && dirY != 0) { dirX *= 0.7071; dirY *= 0.7071; }
                    }

                    // Móng thật thì chôn từ mặt sàn Z=0 đi xuống (có phân tầng nếu bẻ cùng phương)
                    double zEmbed = 0 - input.EmbedLength + layerZOffset; 
                    double xFoot = xNom + (dirX * input.FootLength);
                    double yFoot = yNom + (dirY * input.FootLength);

                    points.Add(new Point3D(xFoot, yFoot, zEmbed)); // Mũi chân
                    points.Add(new Point3D(xNom, yNom, zEmbed));   // Góc ngàm
                    if (Math.Abs(zEmbed) > 1.0)
                    {
                        points.Add(new Point3D(xNom, yNom, 0));    // Mặt sàn (nếu có đoạn cắm sâu)
                    }
                    points.Add(new Point3D(xNom, yNom, zTop));     // Đỉnh
                }
                // Nếu là CỘT TRÊN ÉP THẲNG (Không có râu chờ)
                else
                {
                    if (isUpperColumn)
                    {
                        // Cột trên nối thẳng: Dời tâm vào trong (d + 4mm) để không bị trùng trục với thanh tầng dưới
                        double dirX = 0, dirY = 0;
                        if (Math.Abs(xNom) > 0.1) dirX = (xNom > 0) ? -1 : 1;
                        if (Math.Abs(yNom) > 0.1) dirY = (yNom > 0) ? -1 : 1;
                        if (dirX != 0 && dirY != 0) { dirX *= 0.7071; dirY *= 0.7071; }

                        double offsetIn = input.BarDiameter + 4.0;
                        double xShift = xNom + (dirX * offsetIn);
                        double yShift = yNom + (dirY * offsetIn);

                        points.Add(new Point3D(xShift, yShift, zStart));
                        points.Add(new Point3D(xShift, yShift, zTop));
                    }
                    else
                    {
                        // Cột móng / tầng đáy
                        points.Add(new Point3D(xNom, yNom, zStart));
                        points.Add(new Point3D(xNom, yNom, zTop));
                    }
                }
            }

            // --- XỬ LÝ NEO ĐỈNH (CỘT MÁI / KHÓA ĐẦU CỘT) ---
            if (input.IsTopColumn && input.HasTopHook)
            {
                var pTop = points[points.Count - 1];
                double dirX = 0, dirY = 0;
                if (Math.Abs(xNom) > 0.1) dirX = (xNom > 0) ? -1 : 1;
                if (Math.Abs(yNom) > 0.1) dirY = (yNom > 0) ? -1 : 1;
                if (dirX != 0 && dirY != 0) { dirX *= 0.7071; dirY *= 0.7071; }

                // ĐIỀU KIỆN BIÊN KHÓA ĐẦU CỘT: Mũi móc neo không bao giờ được phép đâm ra ngoài mép cột
                // Giới hạn trong vùng lõi cốt thép [-effW/2, effW/2] và [-effD/2, effD/2]
                double maxHookX = double.MaxValue;
                if (dirX > 0.01) maxHookX = (effW / 2.0 - xNom) / dirX;
                else if (dirX < -0.01) maxHookX = (-effW / 2.0 - xNom) / dirX;

                double maxHookY = double.MaxValue;
                if (dirY > 0.01) maxHookY = (effD / 2.0 - yNom) / dirY;
                else if (dirY < -0.01) maxHookY = (-effD / 2.0 - yNom) / dirY;

                double maxAllowedHook = Math.Min(maxHookX, maxHookY);
                // Giữ khoảng cách an toàn 15mm trước biên bảo vệ đối diện để không bị loài ra ngoài bê tông
                maxAllowedHook = Math.Max(0, maxAllowedHook - 15.0);

                double actualHookLength = Math.Min(input.TopHookLength, maxAllowedHook);

                double xHook = pTop.X + (dirX * actualHookLength);
                double yHook = pTop.Y + (dirY * actualHookLength);
                points.Add(new Point3D(xHook, yHook, pTop.Z));
            }

            return points;
        }
    }
}
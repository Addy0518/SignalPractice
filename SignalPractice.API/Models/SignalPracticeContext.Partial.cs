namespace SignalPractice.API.Models
{
    public partial class SignalPracticeContext
    {
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            // SQL Server 的 datetime/datetime2 欄位不會記錄時區資訊，
            // 導致 EF Core 從資料庫讀出來的 DateTime.Kind 一律變成 Unspecified，
            // 序列化給前端時就不會帶 "Z" 結尾，前端會誤判成本地時間而不是 UTC，
            // 這裡統一在讀取時補標記回 Utc，寫入時則原樣存入不做轉換
            modelBuilder.Entity<Room>(entity =>
            {
                entity
                    .Property(e => e.RoundEndTime)
                    .HasConversion(
                        v => v, // 寫入資料庫：不做任何轉換
                        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v // 讀出來：補標記為 UTC
                    );

                entity
                    .Property(e => e.CreateTime)
                    .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            });
        }
    }
}

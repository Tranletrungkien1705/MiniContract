using Xunit;

// Cac test "RealServer_*" khoi dong Kestrel that tren cong co dinh (nhieu lop dung chung/trung cong, chung DB tam):
// chay song song se dung cong/DB lan nhau -> loi chap chon. Tat song song hoa de ket qua on dinh.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

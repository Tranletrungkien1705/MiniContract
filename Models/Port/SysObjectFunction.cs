using System.Runtime.Serialization;

namespace MiniContract.Models.Port;

// port 1:1 BaseController.cs — SysObjectSetting.config (helper gate, không phải bảng DB)
// Nguồn model: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\SysObjectFunction.cs:9-26
// Nguồn đọc file: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:3780-3795 (GetSysObjectSetting)
// Nguồn dùng:    D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:3843-3894 (ResolveObjects)
[DataContract(Name = "SysObjectSetting", Namespace = "http://schemas.datacontract.org/2004/07/idn.Skycic.QContract.Common.Models")]
public class SysObjectSetting
{
    // BaseController.cs:3847-3850 — danh sách cấu hình ObjectCode -> FunctionCodes
    [DataMember]
    public List<SysObjectFunction>? List { get; set; }
}

[DataContract(Name = "SysObjectFunction", Namespace = "http://schemas.datacontract.org/2004/07/idn.Skycic.QContract.Common.Models")]
public class SysObjectFunction
{
    // SysObjectFunction.cs:13
    [DataMember(Order = 1)]
    public string? ObjectCode { get; set; }

    // SysObjectFunction.cs:17 — mỗi phần tử có thể là chuỗi "a,b" (ResolveObjects tách bằng Split(','), BaseController.cs:3872-3879)
    [DataMember(Order = 10)]
    public List<string>? FunctionCodes { get; set; }
}

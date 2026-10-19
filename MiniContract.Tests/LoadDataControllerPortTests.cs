using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;
using Xunit.Abstractions;

namespace MiniContract.Tests
{
    public class LoadDataControllerPortTests : IDisposable
    {
        private readonly ITestOutputHelper? _output;
        private readonly SqliteConnection _conn;
        private readonly AppDbContext _db;
        private readonly LoadDataControllerPort _controller;

        public LoadDataControllerPortTests(ITestOutputHelper? output = null)
        {
            _output = output;
            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();

            var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
            _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
            _db.Database.EnsureCreated();

            new Mst_ContractTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();
            new Mst_Attribute_ContractSeeder().SeedAsync(_db).GetAwaiter().GetResult();
            new Contract_TempContractSeeder().SeedAsync(_db).GetAwaiter().GetResult();

            _controller = new LoadDataControllerPort(_db);
        }

        public void Dispose()
        {
            _conn.Dispose();
            _db.Dispose();
        }

        private static JsonElement ExtractJson(IActionResult result)
        {
            if (result is OkObjectResult ok)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(ok.Value)).RootElement;
            }
            if (result is BadRequestObjectResult bad)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(bad.Value)).RootElement;
            }
            throw new InvalidOperationException("Unexpected result type: " + result?.GetType().Name);
        }

        [Fact]
        public async Task Contract_TempContract_Create_EmptyBody_ReturnsFail()
        {
            var res = await _controller.Contract_TempContract_Create(null);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_Save", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_MissingTContractCode_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "",
                TContracName = "Mau hop dong",
                ContractType = "HD_KT",
                TContractFileName = "file.docx"
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_SaveX_InvalidTContractCode", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_MissingTContracName_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_TEST_01",
                TContracName = "",
                ContractType = "HD_KT",
                TContractFileName = "file.docx"
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_SaveX_InvalidTContracName", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_ContractTypeNotFound_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_TEST_01",
                TContracName = "Mau hop dong",
                ContractType = "NOT_EXISTING_TYPE",
                TContractFileName = "file.docx"
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_ContractTypeInactive_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_TEST_01",
                TContracName = "Mau hop dong",
                ContractType = "HD_MB", // FlagActive = 0 trong seeder
                TContractFileName = "file.docx"
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_CheckDB_FlagActiveNotMatched", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_DuplicateTContracName_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_TEST_02",
                TContracName = "Hop dong mau 01", // Đã có TCONT001 trong OrgID ORG01
                OrgID = "ORG01",
                ContractType = "HD_KT",
                TContractFileName = "file.docx"
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_SaveX_TContracNameExist", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_MissingFileName_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_TEST_03",
                TContracName = "Mau hop dong moi",
                ContractType = "HD_KT",
                TContractFileName = ""
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Contract_Contract_Approved_InvalidContractFileName", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_InvalidAttributeCode_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_TEST_04",
                TContracName = "Mau hop dong voi thuoc tinh sai",
                ContractType = "HD_KT",
                TContractFileName = "file.docx",
                Lst_Contract_Attribute_Temp = new List<Contract_Attribute_Temp>
                {
                    new Contract_Attribute_Temp { AttributeContractCode = "", AttributeValue = "val" }
                }
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_SaveX_InvalidAttributeContractCode", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_InvalidAttributeValue_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_TEST_04",
                TContracName = "Mau hop dong voi gia tri thuoc tinh rong",
                ContractType = "HD_KT",
                TContractFileName = "file.docx",
                Lst_Contract_Attribute_Temp = new List<Contract_Attribute_Temp>
                {
                    new Contract_Attribute_Temp { AttributeContractCode = "ATTR01", AttributeValue = "   " }
                }
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_SaveX_InvalidAttributeValue", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_AttributeNotFound_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_TEST_04",
                TContracName = "Mau hop dong voi thuoc tinh khong ton tai",
                ContractType = "HD_KT",
                TContractFileName = "file.docx",
                Lst_Contract_Attribute_Temp = new List<Contract_Attribute_Temp>
                {
                    new Contract_Attribute_Temp { AttributeContractCode = "NOT_EXIST", AttributeValue = "val" }
                }
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_InvalidAccessKeyLength_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_TEST_05",
                TContracName = "Mau hop dong voi accesskey ngan",
                ContractType = "HD_KT",
                TContractFileName = "file.docx",
                Lst_Contract_TempContractUser = new List<Contract_TempContractUserReq>
                {
                    new Contract_TempContractUserReq { Idx = "1", UserCodeSign = "U01", AccessKey = "123" }
                }
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_Save_Contract_TempContractUserInvalidAccessKeyLength", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_DuplicateAccessKey_ReturnsFail()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_TEST_06",
                TContracName = "Mau hop dong voi accesskey trung",
                ContractType = "HD_KT",
                TContractFileName = "file.docx",
                Lst_Contract_TempContractUser = new List<Contract_TempContractUserReq>
                {
                    new Contract_TempContractUserReq { Idx = "1", UserCodeSign = "U01", AccessKey = "123456" },
                    new Contract_TempContractUserReq { Idx = "2", UserCodeSign = "U02", AccessKey = "123456" }
                }
            };
            var res = await _controller.Contract_TempContract_Create(req);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var root = ExtractJson(bad);

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_Save_Contract_TempContractUserDuplicateAccessKey", root.GetProperty("ErrorCode").GetString());
        }

        [Fact]
        public async Task Contract_TempContract_Create_Success_RoundTrip()
        {
            var req = new Contract_TempContract_CreateReq
            {
                TContractCode = "TCC_SUCCESS_01",
                TContracName = "Mau hop dong kinh te thuc te",
                ContractName = "Hop dong kinh te",
                ContractType = "HD_KT",
                TContractFileName = "Template_KT.docx",
                TContractFilePath = "/upload/template_kt.docx",
                Remark = "Tao thanh cong",
                FlagActive = "1",
                Lst_Contract_Attribute_Temp = new List<Contract_Attribute_Temp>
                {
                    new Contract_Attribute_Temp { AttributeContractCode = "ATTR01", AttributeValue = "HD-KT-2026-001" }
                },
                Lst_Contract_TempContractParty = new List<Contract_TempContractParty>
                {
                    new Contract_TempContractParty { PartyCode = "PARTY_A", CustomerName = "Cong ty A" }
                }
            };

            var res = await _controller.Contract_TempContract_Create(req);
            var ok = Assert.IsType<OkObjectResult>(res);
            var root = ExtractJson(ok);

            Assert.True(root.GetProperty("Success").GetBoolean());
            var data = root.GetProperty("Data");
            Assert.Equal("TCC_SUCCESS_01", data.GetProperty("TContractCode").GetString());

            // Đọc lại từ DB để kiểm tra tính toàn vẹn (round-trip)
            var dbRecord = await _db.Set<Contract_TempContract>().FirstOrDefaultAsync(x => x.TContractCode == "TCC_SUCCESS_01");
            Assert.NotNull(dbRecord);
            Assert.Equal("Mau hop dong kinh te thuc te", dbRecord.TContracName);
            Assert.Equal("HD_KT", dbRecord.ContractType);
            Assert.Equal("Template_KT.docx", dbRecord.TContractFileName);
            Assert.Equal("1", dbRecord.FlagActive);

            var dbAtt = await _db.Set<Contract_Attribute_Temp>().FirstOrDefaultAsync(x => x.TContractCode == "TCC_SUCCESS_01");
            Assert.NotNull(dbAtt);
            Assert.Equal("ATTR01", dbAtt.AttributeContractCode);
            Assert.Equal("HD-KT-2026-001", dbAtt.AttributeValue);

            var dbParty = await _db.Set<Contract_TempContractParty>().FirstOrDefaultAsync(x => x.TContractCode == "TCC_SUCCESS_01");
            Assert.NotNull(dbParty);
            Assert.Equal("PARTY_A", dbParty.PartyCode);
            Assert.Equal("Cong ty A", dbParty.CustomerName);
        }

        [Fact]
        public async Task Contract_TempContract_Create_ModelJsonString_Success()
        {
            var inner = new
            {
                TContractCode = "TCC_FROM_MODEL",
                TContracName = "Mau hop dong truyen model JSON",
                ContractType = "HD_KT",
                TContractFileName = "model.docx",
                FlagActive = "1"
            };

            var req = new Contract_TempContract_CreateReq
            {
                Model = JsonSerializer.Serialize(inner)
            };

            var res = await _controller.Contract_TempContract_Create(req);
            var ok = Assert.IsType<OkObjectResult>(res);
            var root = ExtractJson(ok);

            Assert.True(root.GetProperty("Success").GetBoolean());
            Assert.Equal("TCC_FROM_MODEL", root.GetProperty("Data").GetProperty("TContractCode").GetString());

            var saved = await _db.Set<Contract_TempContract>().FirstOrDefaultAsync(x => x.TContractCode == "TCC_FROM_MODEL");
            Assert.NotNull(saved);
            Assert.Equal("Mau hop dong truyen model JSON", saved.TContracName);
        }

        [Fact]
        public async Task LiveServer_Contract_TempContract_Create_VerifyRealEndpoint()
        {
            var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_loaddata_{Guid.NewGuid():N}.db");
            var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../bin/Release/net8.0/MiniContract.dll"));
            if (!File.Exists(dllPath))
            {
                dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
            }
            if (!File.Exists(dllPath))
            {
                dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "MiniContract.dll"));
            }
            Assert.True(File.Exists(dllPath), $"Cannot find MiniContract.dll at {dllPath}");

            var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(dllPath),
            };
            var freePort = FreePort.Get();
            psi.Environment["PORT"] = freePort.ToString();
            psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

            using var proc = Process.Start(psi);
            Assert.NotNull(proc);

            try
            {
                using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort}"), Timeout = TimeSpan.FromSeconds(3) };
                bool started = false;
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(500);
                    try
                    {
                        var res = await http.GetAsync("/healthz");
                        if (res.IsSuccessStatusCode)
                        {
                            started = true;
                            break;
                        }
                    }
                    catch { }
                }
                Assert.True(started, "MiniContract server failed to start within 15 seconds.");

                // 1. Guard Ca 1: Body rỗng -> HTTP 400 và mã lỗi Contract_TempContract_Save
                var reqEmpty = new StringContent("{}", Encoding.UTF8, "application/json");
                var resEmpty = await http.PostAsync("/api/loaddata/contract_tempcontract_create", reqEmpty);
                var bodyEmpty = await resEmpty.Content.ReadAsStringAsync();
                var docEmpty = JsonDocument.Parse(bodyEmpty);
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
                Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_Save", docEmpty.RootElement.GetProperty("ErrorCode").GetString());
                _output?.WriteLine($"[Guard 1 Empty Body] StatusCode={(int)resEmpty.StatusCode} Body={bodyEmpty}");

                // 2. Guard Ca 2: Thiếu TContracName -> HTTP 400 và mã lỗi Contract_TempContract_SaveX_InvalidTContracName
                var reqMissingName = new StringContent("{\"TContractCode\":\"TCC_LIVE_FAIL\",\"TContracName\":\"\",\"ContractType\":\"HD_KT\",\"TContractFileName\":\"file.docx\"}", Encoding.UTF8, "application/json");
                var resMissingName = await http.PostAsync("/api/loaddata/contract_tempcontract_create", reqMissingName);
                var bodyMissingName = await resMissingName.Content.ReadAsStringAsync();
                var docMissingName = JsonDocument.Parse(bodyMissingName);
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, resMissingName.StatusCode);
                Assert.False(docMissingName.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_SaveX_InvalidTContracName", docMissingName.RootElement.GetProperty("ErrorCode").GetString());
                _output?.WriteLine($"[Guard 2 Missing Name] StatusCode={(int)resMissingName.StatusCode} Body={bodyMissingName}");

                // 3. Round-trip Tạo mới -> HTTP 200 và Success = true
                var reqCreate = new StringContent(
                    "{\"TContractCode\":\"TCC_LIVE_01\",\"TContracName\":\"Hop dong mau live server\",\"ContractName\":\"Hop dong live\",\"ContractType\":\"HD_KT\",\"TContractFileName\":\"template_live.docx\",\"FlagActive\":\"1\"}",
                    Encoding.UTF8, "application/json");
                var resCreate = await http.PostAsync("/api/loaddata/contract_tempcontract_create", reqCreate);
                var bodyCreate = await resCreate.Content.ReadAsStringAsync();
                var docCreate = JsonDocument.Parse(bodyCreate);
                Assert.Equal(System.Net.HttpStatusCode.OK, resCreate.StatusCode);
                Assert.True(docCreate.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("TCC_LIVE_01", docCreate.RootElement.GetProperty("Data").GetProperty("TContractCode").GetString());
                _output?.WriteLine($"[Round-trip Create] StatusCode={(int)resCreate.StatusCode} Body={bodyCreate}");

                // 4. Round-trip Sửa/Cập nhật với cùng mã -> HTTP 200 và TContracName được cập nhật
                var reqUpdate = new StringContent(
                    "{\"TContractCode\":\"TCC_LIVE_01\",\"TContracName\":\"Hop dong mau live server UPDATED\",\"ContractName\":\"Hop dong live v2\",\"ContractType\":\"HD_KT\",\"TContractFileName\":\"template_live_v2.docx\",\"FlagActive\":\"1\"}",
                    Encoding.UTF8, "application/json");
                var resUpdate = await http.PostAsync("/api/loaddata/contract_tempcontract_create", reqUpdate);
                var bodyUpdate = await resUpdate.Content.ReadAsStringAsync();
                var docUpdate = JsonDocument.Parse(bodyUpdate);
                Assert.Equal(System.Net.HttpStatusCode.OK, resUpdate.StatusCode);
                Assert.True(docUpdate.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Hop dong mau live server UPDATED", docUpdate.RootElement.GetProperty("Data").GetProperty("TContracName").GetString());
                _output?.WriteLine($"[Round-trip Update] StatusCode={(int)resUpdate.StatusCode} Body={bodyUpdate}");
            }
            finally
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                    }
                }
                catch { }

                try
                {
                    if (File.Exists(tempDb))
                    {
                        File.Delete(tempDb);
                    }
                }
                catch { }
            }
        }
    }
}

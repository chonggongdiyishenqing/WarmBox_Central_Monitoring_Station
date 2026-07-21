using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Efferent.HL7.V2;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.ViewModel;

namespace WarmBox_Central_Monitoring_Station.Services
{
    public static class Hl7DataProcessor
    {
        // ========== 参数名 → BedViewModel 属性名映射（完全对应原 ObxFieldMap） ==========
        private static readonly Dictionary<string, string> ParamMap = new()
        {
            { "AIR",      "BoxTemp" },
            { "SKIN",     "SkinTemp1" },
            { "SKIN2",    "SkinTemp2" },
            { "HUM",      "Humidity" },
            { "O2",       "O2" },
            { "SPO2",     "BloodOxygen" },    // 血氧
            { "PI",       "PI" },
            { "SpHb",     "SpHb" },
            { "SpOC",     "SpOC" },
            { "SpMet",    "SpMet" },
            { "SpCO",     "SpCO" },
            { "PVI",      "PV1" },
            { "HR",       "HeartRate" },
            { "PR",       "PR" },
            { "RESP",     "RespirationRate" },
            { "NIBP_S",   "NIBP_SYS" },
            { "NIBP_D",   "NIBP_DIA" },
            { "SC",       "Weight" },         // 体重
            { "INC",      "INC" },            // 临时变量，同时会在 UI 中显示
            { "WARM",     "WARM_Percent" },   // 保暖台功率
            { "W_MAN",    "W_MAN" },          // 手控功率
            { "C_AIR",    "C_AIR" },
            { "C_HUM",    "C_HUM" },
            { "C_O?",     "C_O2" },
            { "C_SKIN",   "C_SKIN" },
            { "W_SKIN",   "W_SKIN" },
            { "G1",       "G1" },
            { "W1",       "W1" },
            { "LED",      "LED" },
            { "STATUS",   "STATUS" },
            { "kangaroo_mode", "KangarooMode" },
            { "Qibo",     "Qibo" },
            { "WAKE",  "WAKE"}// 若设备发送
        };

        // ========== 报警代码映射（复用原有完整字典，因篇幅限制仅展示部分，实际请用完整版） ==========
        private static readonly Dictionary<string, string> AlarmCodeMap = new()
        {
            {"SYS.CON","系统板与主板通讯故障"},{"SYS.BCON","控制仪与传感器盒通讯故障"},
            {"SYS.HA","主从机通讯故障"},{"SYS.HAC","从机间通讯故障"},
            {"SYS.CFG","备份配置数据异常"},{"SYS.HOOD","箱篷故障"},
            {"SYS.MOTOR","升降系统故障"},{"SYS.DOOR","辐射门故障"},
            {"SYS.WARM1","培养箱加热系统故障"},{"SYS.WARM2","辐射加热系统故障"},
            {"SYS.WARM3","床垫加热系统故障"},{"SYS.BOX","传感器盒放置错误报警"},
            {"SEN.A1","箱温传感器故障"},{"SEN.A2","独立箱温传感器故障"},
            {"SEN.ADIF","箱温传感器差异"},{"SEN.F1","风道温度传感器故障"},
            {"SEN.M1","床温传感器故障"},{"SEN.M2","床温独立传感器故障"},
            {"SEN.MDIF","床温传感器差异故障"},{"SEN.S1A","肤温传感器1故障"},
            {"SEN.S1B","独立肤温传感器故障"},{"SEN.S2","肤温传感器2故障"},
            {"SEN.SDIF","肤温传感器差异"},{"SEN.SCALE","体重秤传感器未连接"},
            {"AIR.OVH","箱温超温"},{"MAT.OVH","床温超温"},
            {"SKIN.OVH","肤温超温"},{"FLOW.OVH","风道超温"},
            {"CTRL.SEN","肤温传感器放置错误"},{"SYS.FAN","风机故障"},
            {"UNIT.FAN","控制仪内风扇故障"},{"SEN.O2_1","O₂传感器1故障"},
            {"SEN.O2_2","O₂传感器2故障"},{"SEN.O2DIF","O₂传感器差异"},
            {"SEN.RAIN","血氧模块通信中断"},{"RAIN.ERR","血氧系统故障"},
            {"RAIN.DF","血氧诊断故障"},{"RAIN.E0","血氧导电线未连接"},
            {"RAIN.E1","血氧导电线过期"},{"RAIN.E2","血氧导电线不匹配"},
            {"RAIN.E3","血氧无法识别导电线"},{"RAIN.E4","血氧导电线故障"},
            {"RAIN.E7","血氧传感器连接错误"},{"RAIN.E8","血氧传感器过期"},
            {"RAIN.E11","血氧传感器故障"},{"RAIN.E10","血氧传感器无法识别"},
            {"RAIN.E12","检查血氧导电线或传感器"},{"RAIN.E15","未连接血氧粘黏探头"},
            {"RAIN.E16","血氧粘黏探头过期"},{"RAIN.E17","血氧粘黏探头不匹配"},
            {"RAIN.E18","无法识别血氧粘黏探头"},{"RAIN.E19","血氧粘黏探头故障"},
            {"RAIN.E21","血氧传感器脱落"},{"RAIN.E28","检查血氧传感器连接"},
            {"ECG.CON","ECG通信中断"},{"ECG.RESP","RESP通信中断"},
            {"NIBP.DIS","NIBP模块禁用"},{"NIBP.CON","NIBP通信中断"},
            {"NIBP.SEL","NIBP自检失败"},{"WAKE.CON","唤醒模块通信中断"},
            {"WAKE.ERR","唤醒器连接错误"},{"PRINT.ERR","打印机错误"},
            {"PRINT.CON","打印机通信中断"},{"SYS.HUMC","湿度加热系统故障报警"},
            {"SEN.HUM","湿度传感器报警"},{"LOW.POWER","低压报警"},
            {"BAT.CON","电池连接故障"},{"SYS.UPS","镍氢电池报警"},
            {"LIBAT1.LOW","锂电池1电压过低"},{"LIBAT2.LOW","锂电池2电压过低"},
            {"LIBAT1.HARD","锂电池1硬件故障"},{"LIBAT2.HARD","锂电池2硬件故障"},
            {"LIBAT.LOW","锂电池电量低"},{"LIBAT.HARD","锂电池硬件故障"},
            {"ECG.DROP","ECG导联脱落"},{"ECG.VDROP","ECG V 导联脱落"},
            {"ECG.OV","ECG过载"},{"NIBP.CUFF1","NIBP袖带错误"},
            {"NIBP.CUFF2","NIBP袖带微弱"},{"NIBP.OV1","NIBP测量超界"},
            {"NIBP.OV2","NIBP袖带过压"},{"NIBP.OV3","NIBP测量超时"},
            {"NIBP.SIG","NIBP信号饱和"},{"NIBP.ABO","NIBP测量中断"},
            {"NIBP.TYPE","NIBP袖带类型错误"},{"NIBP.LEAK","NIBP袖带漏气"},
            {"NIBP.PRE","NIBP气动堵塞"},{"NIBP.MOV","NIBP过分运动"},
            {"NIBP.HARD","NIBP硬件错误"},{"CO2.HARD","CO₂硬件错误"},
            {"CO2.SOFT","CO₂软件错误"},{"CO2.SPE","CO₂电机转速超限"},
            {"CO2.NCAL","CO₂出厂未校准"},{"CO2.CLOG","CO₂采样管堵塞"},
            {"CO2.NCLOG","CO₂没有采样管"},{"CO2.OUTA","CO₂超出精度范围"},
            {"CO2.OUTT","CO₂温度超界"},{"CO2.OUTP","CO₂大气压力超限"},
            {"CO2.ZR","CO₂需要校零"},{"CO2.ZSD","CO₂禁止校零"},
            {"CO2.ZIP","CO₂正在校零"},{"CO2.SCF","CO₂校准失败"},
            {"CO2.SCIP","CO₂正在校准"},{"PRINT.PAPER","打印机缺纸"},
            {"SYS.TANK","水箱放置错误报警"},{"SYS.WATER","缺水报警"},
            {"SP.E0","血氧低信号质量"},{"SP.E2","无效的SpO₂"},
            {"PR.E0","低可信度的PR"},{"PR.E2","无效的PR"},
            {"PI.E0","低可信度的PI"},{"PI.E1","无效的PI"},
            {"PI.E2","无效平滑的PI"},{"CO.E0","低可信度的SpCO"},
            {"CO.E1","低血流灌注SpCO"},{"CO.E2","无效的SpCO"},
            {"MET.E0","低可信度的SpMet"},{"MET.E1","低血流灌注SpMet"},
            {"MET.E2","无效的SpMet"},{"HB.E0","低可信度的SpHb"},
            {"HB.E1","低血流灌注SpHb"},{"HB.E2","无效的SpHb"},
            {"OC.E0","低可信度的SpOC"},{"OC.E1","低血流灌注SpOC"},
            {"OC.E2","无效的SpOC"},{"PVI.E0","低可信度的PVI"},
            {"PVI.E2","无效的PVI"},{"RAIN.E24","低血流灌注"},
            {"RAIN.E20","血氧传感器初始化"},{"RAIN.E22","搜寻脉搏"},
            {"RAIN.E23","血氧探测到干扰"},{"RAIN.E26","血氧粘黏探头将要过期"},
            {"SYS.ANG","摇床板与主板通信故障"},{"RAIN.E29","仅限SpO₂模式"},
            {"RAIN.E5","血氧导电线将要过期"},{"RAIN.E9","血氧传感器不匹配"},
            {"RAIN.E14","血氧传感器将要过期"},{"DOOR.OPEN","前门打开"},
            {"TEMP.DEVH","温度上偏差"},{"TEMP.DEVL","温度下偏差"},
            {"MAT.DEVH","床温上偏差"},{"MAT.DEVL","床温下偏差"},
            {"CTRL.ERR1","设置报警A"},{"CTRL.ERR2","设置报警C"},
            {"CTRL.MAN1","请检查肤温"},{"CTRL.MAN","手控检查报警"},
            {"O2.DEVH","O₂上偏差"},{"O2.DEVL","O₂下偏差"},
            {"ECG.ASY","停搏"},{"ECG.VEN","室颤/室速"},
            {"ECG.APN","窒息"},{"SP.OVH","SpO₂高"},
            {"SP.OVL","SpO₂低"},{"PR.OVH","PR高"},
            {"PR.OVL","PR低"},{"HB.OVH","SpHb高"},
            {"HB.OVL","SpHb低"},{"OC.OVH","SpOC高"},
            {"OC.OVL","SpOC低"},{"MET.OVH","SpMet高"},
            {"MET.OVL","SpMet低"},{"CO.OVH","SpCO高"},
            {"CO.OVL","SpCO低"},{"PI.OVH","PI高"},
            {"PI.OVL","PI低"},{"PVI.OVH","PVI高"},
            {"PVI.OVL","PVI低"},{"ECG.HRH","心率高"},
            {"ECG.HRL","心率低"},{"ECG.RRH","呼吸率高"},
            {"ECG.RRL","呼吸率低"},{"NIBP.SH","NIBP收缩压高"},
            {"NIBP.SL","NIBP收缩压低"},{"NIBP.DH","NIBP舒张压高"},
            {"NIBP.DL","NIBP舒张压低"},{"NIBP.MH","NIBP平均压高"},
            {"NIBP.ML","NIBP平均压低"},{"CO2.CON","CO₂通信中断"},
            {"CO2.ETH","EtCO₂高"},{"CO2.ETL","EtCO₂低"},
            {"CO2.FIH","FiCO₂高"},{"CO2.FIL","FiCO₂低"},
            {"CO2.BRH","BR高"},{"CO2.BRL","BR低"},
            {"HUM.DEVH","湿度上偏差"},{"HUM.DEVL","湿度下偏差"},
            {"SPO2.LOW","血氧饱和度低于85%"}
        };

        private static readonly Dictionary<string, string> AlarmTypeMap = new()
        {
            // 技术报警 (T)
            {"SYS.CON","技术报警"},{"SYS.BCON","技术报警"},{"SYS.HA","技术报警"},{"SYS.HAC","技术报警"},
            {"SYS.CFG","技术报警"},{"SYS.HOOD","技术报警"},{"SYS.MOTOR","技术报警"},{"SYS.DOOR","技术报警"},
            {"SYS.WARM1","技术报警"},{"SYS.WARM2","技术报警"},{"SYS.WARM3","技术报警"},{"SYS.BOX","技术报警"},
            {"SEN.A1","技术报警"},{"SEN.A2","技术报警"},{"SEN.ADIF","技术报警"},{"SEN.F1","技术报警"},
            {"SEN.M1","技术报警"},{"SEN.M2","技术报警"},{"SEN.MDIF","技术报警"},{"SEN.S1A","技术报警"},
            {"SEN.S1B","技术报警"},{"SEN.S2","技术报警"},{"SEN.SDIF","技术报警"},{"SEN.SCALE","技术报警"},
            {"AIR.OVH","技术报警"},{"MAT.OVH","技术报警"},{"SKIN.OVH","技术报警"},{"FLOW.OVH","技术报警"},
            {"CTRL.SEN","技术报警"},{"SYS.FAN","技术报警"},{"UNIT.FAN","技术报警"},{"SEN.O2_1","技术报警"},
            {"SEN.O2_2","技术报警"},{"SEN.O2DIF","技术报警"},{"SEN.RAIN","技术报警"},{"RAIN.ERR","技术报警"},
            {"RAIN.DF","技术报警"},{"RAIN.E0","技术报警"},{"RAIN.E1","技术报警"},{"RAIN.E2","技术报警"},
            {"RAIN.E3","技术报警"},{"RAIN.E4","技术报警"},{"RAIN.E7","技术报警"},{"RAIN.E8","技术报警"},
            {"RAIN.E11","技术报警"},{"RAIN.E10","技术报警"},{"RAIN.E12","技术报警"},{"RAIN.E15","技术报警"},
            {"RAIN.E16","技术报警"},{"RAIN.E17","技术报警"},{"RAIN.E18","技术报警"},{"RAIN.E19","技术报警"},
            {"RAIN.E21","技术报警"},{"RAIN.E28","技术报警"},{"ECG.CON","技术报警"},{"NIBP.DIS","技术报警"},
            {"NIBP.CON","技术报警"},{"NIBP.SEL","技术报警"},{"WAKE.CON","技术报警"},{"WAKE.ERR","技术报警"},
            {"PRINT.ERR","技术报警"},{"PRINT.CON","技术报警"},{"SYS.HUMC","技术报警"},{"SEN.HUM","技术报警"},
            {"LOW.POWER","技术报警"},{"BAT.CON","技术报警"},{"SYS.UPS","技术报警"},{"LIBAT1.LOW","技术报警"},
            {"LIBAT2.LOW","技术报警"},{"LIBAT1.HARD","技术报警"},{"LIBAT2.HARD","技术报警"},{"LIBAT.LOW","技术报警"},
            {"LIBAT.HARD","技术报警"},{"ECG.DROP","技术报警"},{"ECG.VDROP","技术报警"},{"ECG.OV","技术报警"},
            {"NIBP.CUFF1","技术报警"},{"NIBP.CUFF2","技术报警"},{"NIBP.OV1","技术报警"},{"NIBP.OV2","技术报警"},
            {"NIBP.OV3","技术报警"},{"NIBP.SIG","技术报警"},{"NIBP.ABO","技术报警"},{"NIBP.TYPE","技术报警"},
            {"NIBP.LEAK","技术报警"},{"NIBP.PRE","技术报警"},{"NIBP.MOV","技术报警"},{"NIBP.HARD","技术报警"},
            {"CO2.HARD","技术报警"},{"CO2.SOFT","技术报警"},{"CO2.SPE","技术报警"},{"CO2.NCAL","技术报警"},
            {"CO2.CLOG","技术报警"},{"CO2.NCLOG","技术报警"},{"CO2.OUTA","技术报警"},{"CO2.OUTT","技术报警"},
            {"CO2.OUTP","技术报警"},{"CO2.ZR","技术报警"},{"CO2.ZSD","技术报警"},{"CO2.ZIP","技术报警"},
            {"CO2.SCF","技术报警"},{"CO2.SCIP","技术报警"},{"PRINT.PAPER","技术报警"},{"SYS.TANK","技术报警"},
            {"SYS.WATER","技术报警"},{"SP.E0","技术报警"},{"RAIN.E24","技术报警"},{"RAIN.E20","技术报警"},
            {"RAIN.E22","技术报警"},{"RAIN.E23","技术报警"},{"RAIN.E26","技术报警"},{"SYS.ANG","技术报警"},
            {"RAIN.E29","技术报警"},{"RAIN.E5","技术报警"},{"RAIN.E9","技术报警"},{"RAIN.E14","技术报警"},
            {"DOOR.OPEN","技术报警"},{"CO2.CON","技术报警"},
        
            // 生理报警 (P)
            {"TEMP.DEVH","生理报警"},{"TEMP.DEVL","生理报警"},{"MAT.DEVH","生理报警"},{"MAT.DEVL","生理报警"},
            {"CTRL.ERR1","生理报警"},{"CTRL.ERR2","生理报警"},{"CTRL.MAN1","生理报警"},{"CTRL.MAN","生理报警"},
            {"O2.DEVH","生理报警"},{"O2.DEVL","生理报警"},{"ECG.ASY","生理报警"},{"ECG.VEN","生理报警"},
            {"ECG.APN","生理报警"},{"SP.OVH","生理报警"},{"SP.OVL","生理报警"},{"PR.OVH","生理报警"},
            {"PR.OVL","生理报警"},{"HB.OVH","生理报警"},{"HB.OVL","生理报警"},{"OC.OVH","生理报警"},
            {"OC.OVL","生理报警"},{"MET.OVH","生理报警"},{"MET.OVL","生理报警"},{"CO.OVH","生理报警"},
            {"CO.OVL","生理报警"},{"PI.OVH","生理报警"},{"PI.OVL","生理报警"},{"PVI.OVH","生理报警"},
            {"PVI.OVL","生理报警"},{"ECG.HRH","生理报警"},{"ECG.HRL","生理报警"},{"ECG.RRH","生理报警"},
            {"ECG.RRL","生理报警"},{"NIBP.SH","生理报警"},{"NIBP.SL","生理报警"},{"NIBP.DH","生理报警"},
            {"NIBP.DL","生理报警"},{"NIBP.MH","生理报警"},{"NIBP.ML","生理报警"},{"CO2.ETH","生理报警"},
            {"CO2.ETL","生理报警"},{"CO2.FIH","生理报警"},{"CO2.FIL","生理报警"},{"CO2.BRH","生理报警"},
            {"CO2.BRL","生理报警"},{"HUM.DEVH","生理报警"},{"HUM.DEVL","生理报警"},{"SPO2.LOW","生理报警"},
            {"SP.E2","生理报警"},{"PR.E0","生理报警"},{"PR.E2","生理报警"},{"PI.E0","生理报警"},
            {"PI.E1","生理报警"},{"PI.E2","生理报警"},{"CO.E0","生理报警"},{"CO.E1","生理报警"},
            {"CO.E2","生理报警"},{"MET.E0","生理报警"},{"MET.E1","生理报警"},{"MET.E2","生理报警"},
            {"HB.E0","生理报警"},{"HB.E1","生理报警"},{"HB.E2","生理报警"},{"OC.E0","生理报警"},
            {"OC.E1","生理报警"},{"OC.E2","生理报警"},{"PVI.E0","生理报警"},{"PVI.E2","生理报警"},
            {"ECG.RESP","生理报警"},
        };

        private static readonly Dictionary<string, AlarmPriority> AlarmCodePriorityMap = new()
{
    // High
    {"SYS.CON",AlarmPriority.High},{"SYS.BCON",AlarmPriority.High},{"SYS.HA",AlarmPriority.High},
    {"SYS.HAC",AlarmPriority.High},{"SYS.CFG",AlarmPriority.High},{"SYS.HOOD",AlarmPriority.High},
    {"SYS.MOTOR",AlarmPriority.High},{"SYS.DOOR",AlarmPriority.High},
    {"SYS.WARM1",AlarmPriority.High},{"SYS.WARM2",AlarmPriority.High},{"SYS.WARM3",AlarmPriority.High},
    {"SYS.BOX",AlarmPriority.High},{"SEN.A1",AlarmPriority.High},{"SEN.A2",AlarmPriority.High},
    {"SEN.ADIF",AlarmPriority.High},{"SEN.F1",AlarmPriority.High},{"SEN.M1",AlarmPriority.High},
    {"SEN.M2",AlarmPriority.High},{"SEN.MDIF",AlarmPriority.High},{"SEN.S1A",AlarmPriority.High},
    {"SEN.S1B",AlarmPriority.High},{"SEN.S2",AlarmPriority.High},{"SEN.SDIF",AlarmPriority.High},
    {"AIR.OVH",AlarmPriority.High},{"MAT.OVH",AlarmPriority.High},{"SKIN.OVH",AlarmPriority.High},
    {"FLOW.OVH",AlarmPriority.High},{"CTRL.SEN",AlarmPriority.High},{"SYS.FAN",AlarmPriority.High},
    {"UNIT.FAN",AlarmPriority.High},{"SEN.O2_1",AlarmPriority.High},{"SEN.O2_2",AlarmPriority.High},
    {"SEN.O2DIF",AlarmPriority.High},{"SEN.RAIN",AlarmPriority.High},{"RAIN.ERR",AlarmPriority.High},
    {"RAIN.DF",AlarmPriority.High},{"RAIN.E0",AlarmPriority.High},{"RAIN.E1",AlarmPriority.High},
    {"RAIN.E2",AlarmPriority.High},{"RAIN.E3",AlarmPriority.High},{"RAIN.E4",AlarmPriority.High},
    {"RAIN.E7",AlarmPriority.High},{"RAIN.E8",AlarmPriority.High},{"RAIN.E11",AlarmPriority.High},
    {"RAIN.E10",AlarmPriority.High},{"RAIN.E12",AlarmPriority.High},{"RAIN.E15",AlarmPriority.High},
    {"RAIN.E16",AlarmPriority.High},{"RAIN.E17",AlarmPriority.High},{"RAIN.E18",AlarmPriority.High},
    {"RAIN.E19",AlarmPriority.High},{"RAIN.E21",AlarmPriority.High},{"RAIN.E28",AlarmPriority.High},
    {"ECG.CON",AlarmPriority.High},{"NIBP.CON",AlarmPriority.High},{"NIBP.SEL",AlarmPriority.High},
    {"WAKE.CON",AlarmPriority.High},{"WAKE.ERR",AlarmPriority.High},{"PRINT.ERR",AlarmPriority.High},
    {"PRINT.CON",AlarmPriority.High},{"LOW.POWER",AlarmPriority.High},{"BAT.CON",AlarmPriority.High},
    {"SYS.UPS",AlarmPriority.High},{"LIBAT1.LOW",AlarmPriority.High},{"LIBAT2.LOW",AlarmPriority.High},
    {"LIBAT1.HARD",AlarmPriority.High},{"LIBAT2.HARD",AlarmPriority.High},{"LIBAT.LOW",AlarmPriority.High},
    {"LIBAT.HARD",AlarmPriority.High},{"CO2.CON",AlarmPriority.High},
    {"TEMP.DEVH",AlarmPriority.High},{"TEMP.DEVL",AlarmPriority.High},
    {"MAT.DEVH",AlarmPriority.High},{"MAT.DEVL",AlarmPriority.High},
    {"CTRL.ERR1",AlarmPriority.High},{"CTRL.ERR2",AlarmPriority.High},
    {"CTRL.MAN1",AlarmPriority.High},{"CTRL.MAN",AlarmPriority.High},
    {"O2.DEVH",AlarmPriority.High},{"O2.DEVL",AlarmPriority.High},
    {"ECG.ASY",AlarmPriority.High},{"ECG.VEN",AlarmPriority.High},{"ECG.APN",AlarmPriority.High},
    // Medium
    {"SYS.HUMC",AlarmPriority.Medium},{"SEN.HUM",AlarmPriority.Medium},
    {"ECG.DROP",AlarmPriority.Medium},{"ECG.VDROP",AlarmPriority.Medium},
    {"ECG.OV",AlarmPriority.Medium},{"NIBP.CUFF1",AlarmPriority.Medium},
    {"NIBP.CUFF2",AlarmPriority.Medium},{"NIBP.OV1",AlarmPriority.Medium},
    {"NIBP.OV2",AlarmPriority.Medium},{"NIBP.OV3",AlarmPriority.Medium},
    {"NIBP.SIG",AlarmPriority.Medium},{"NIBP.ABO",AlarmPriority.Medium},
    {"NIBP.TYPE",AlarmPriority.Medium},{"NIBP.LEAK",AlarmPriority.Medium},
    {"NIBP.PRE",AlarmPriority.Medium},{"NIBP.MOV",AlarmPriority.Medium},
    {"NIBP.HARD",AlarmPriority.Medium},{"CO2.HARD",AlarmPriority.Medium},
    {"CO2.SOFT",AlarmPriority.Medium},{"CO2.SPE",AlarmPriority.Medium},
    {"CO2.NCAL",AlarmPriority.Medium},{"CO2.CLOG",AlarmPriority.Medium},
    {"CO2.NCLOG",AlarmPriority.Medium},{"CO2.OUTA",AlarmPriority.Medium},
    {"CO2.OUTT",AlarmPriority.Medium},{"CO2.OUTP",AlarmPriority.Medium},
    {"CO2.ZR",AlarmPriority.Medium},{"CO2.ZSD",AlarmPriority.Medium},
    {"CO2.ZIP",AlarmPriority.Medium},{"CO2.SCF",AlarmPriority.Medium},
    {"CO2.SCIP",AlarmPriority.Medium},{"PRINT.PAPER",AlarmPriority.Medium},
    {"SP.OVH",AlarmPriority.Medium},{"SP.OVL",AlarmPriority.Medium},
    {"PR.OVH",AlarmPriority.Medium},{"PR.OVL",AlarmPriority.Medium},
    {"HB.OVH",AlarmPriority.Medium},{"HB.OVL",AlarmPriority.Medium},
    {"OC.OVH",AlarmPriority.Medium},{"OC.OVL",AlarmPriority.Medium},
    {"MET.OVH",AlarmPriority.Medium},{"MET.OVL",AlarmPriority.Medium},
    {"CO.OVH",AlarmPriority.Medium},{"CO.OVL",AlarmPriority.Medium},
    {"PI.OVH",AlarmPriority.Medium},{"PI.OVL",AlarmPriority.Medium},
    {"PVI.OVH",AlarmPriority.Medium},{"PVI.OVL",AlarmPriority.Medium},
    {"ECG.HRH",AlarmPriority.Medium},{"ECG.HRL",AlarmPriority.Medium},
    {"ECG.RRH",AlarmPriority.Medium},{"ECG.RRL",AlarmPriority.Medium},
    {"NIBP.SH",AlarmPriority.Medium},{"NIBP.SL",AlarmPriority.Medium},
    {"NIBP.DH",AlarmPriority.Medium},{"NIBP.DL",AlarmPriority.Medium},
    {"NIBP.MH",AlarmPriority.Medium},{"NIBP.ML",AlarmPriority.Medium},
    {"CO2.ETH",AlarmPriority.Medium},{"CO2.ETL",AlarmPriority.Medium},
    {"CO2.FIH",AlarmPriority.Medium},{"CO2.FIL",AlarmPriority.Medium},
    {"CO2.BRH",AlarmPriority.Medium},{"CO2.BRL",AlarmPriority.Medium},
    {"SPO2.LOW",AlarmPriority.Medium},
    // Low (包含原 Info)
    {"SYS.TANK",AlarmPriority.Low},{"SYS.WATER",AlarmPriority.Low},
    {"SP.E0",AlarmPriority.Low},{"HUM.DEVH",AlarmPriority.Low},{"HUM.DEVL",AlarmPriority.Low},
    {"RAIN.E24",AlarmPriority.Low},{"RAIN.E20",AlarmPriority.Low},{"RAIN.E22",AlarmPriority.Low},
    {"RAIN.E23",AlarmPriority.Low},{"RAIN.E26",AlarmPriority.Low},{"SYS.ANG",AlarmPriority.Low},
    {"RAIN.E29",AlarmPriority.Low},{"RAIN.E5",AlarmPriority.Low},{"RAIN.E9",AlarmPriority.Low},
    {"RAIN.E14",AlarmPriority.Low},{"DOOR.OPEN",AlarmPriority.Low}
};
        private static void LogAlarms(BedViewModel bed, List<string> alarmCodes, List<string> alarmNames, List<string> alarmTypes)
        {
            if (alarmCodes.Count == 0) return;

            for (int i = 0; i < alarmCodes.Count; i++)
            {
                string type = i < alarmTypes.Count ? alarmTypes[i] : "生理报警";
                var priority = AlarmCodePriorityMap.TryGetValue(alarmCodes[i], out var p) ? p : AlarmPriority.Medium;
                string level = priority switch
                {
                    AlarmPriority.High => "紧急",
                    AlarmPriority.Medium => "警告",
                    AlarmPriority.Low => "轻微",
                    _ => "警告"
                };

                var log = new AlarmLogItem
                {
                    BedNumber = bed.BedNumber,
                    PatientName = bed.PatientNameForDisplay,
                    AlarmContent = alarmNames[i],
                    AlarmType = type,
                    AlarmLevel = level,
                    AlarmTime = DateTime.Now,
                    DeviceModel = bed.DeviceType,
                    ParameterName = null,
                    CurrentValue = null
                };
                AlarmLogService.Instance.AddLog(log);
            }
        }
        // ========== 公共解析入口（线程安全，由UI线程调用） ==========
        public static void ProcessMessage(string clientIp, string hl7Message, BedViewModel bed)
        {
            try
            {
                Message message;
                try
                {
                    message = new Message(hl7Message);
                    if (!message.ParseMessage()) return;
                }
                catch { return; }

                // 1. 患者信息
                ParsePatientInfo(message, bed);

                // 2. 遍历 OBX
                var alarmNames = new List<string>();
                var alarmTypesForLog = new List<string>();  // 与 alarmNames 并列
                string ctrlMode = null;
                string statusValue = null;
                bool isKangaroo = false;
                double? incVal = null, warmVal = null, wmanVal = null;
                bool hasNibpSys = false, hasNibpDia = false;
                string nibpSys = null, nibpDia = null;
                List<string> alarmCodes = new();

                foreach (var obx in message.Segments("OBX"))
                {
                    string obx2 = obx.Fields(2).Value;
                    string obx3 = obx.Fields(3).Value;
                    string obx5 = obx.Fields(5).Value;
                    string obx7 = obx.Fields(7).Value;
                    string paramName = ExtractParamName(obx3);

                    // 患者信息段 (子ID 1-4)
                    if (paramName == "GESTATION") { bed.PatientTaiLing = obx5; continue; }
                    if (paramName == "BLOOD_TYPE") { bed.BloodType = obx5; continue; }
                    if (paramName == "HEIGHT") { bed.Height = obx5; continue; }
                    if (paramName == "BIRTH_WEIGHT") { bed.Weight = obx5; continue; }

                    // 波形提取
                    if (TryExtractWaveform(obx3, obx5, bed)) continue;

                    // 报警
                    if (obx2 == "ST" && paramName.Contains("ALARM"))
                    {
                        var codes = obx5.Split(':', '，', ',');
                        foreach (var raw in codes)
                        {
                            string code = raw.Trim();
                            if (string.IsNullOrEmpty(code)) continue;
                            alarmCodes.Add(code);
                            string name = AlarmCodeMap.TryGetValue(code, out var n) ? n : code;
                            alarmNames.Add(name);
                            // 记录报警类型（用于日志）
                            alarmTypesForLog.Add(AlarmTypeMap.TryGetValue(code, out var t) ? t : "生理报警");
                        }
                        continue;
                    }

                    // 工作模式 / 状态
                    if (paramName == "CTRL") { ctrlMode = obx5?.Trim(); continue; }
                    if (paramName == "STATUS") { statusValue = obx5?.Trim(); continue; }
                    if (paramName == "kangaroo_mode") { isKangaroo = obx5?.Trim() == "1"; bed.KangarooMode = isKangaroo ? "开" : "关"; continue; }

                    // 数值参数
                    if (ParamMap.TryGetValue(paramName, out string propName))
                    {
                        object val = ParseNumeric(obx5);
                        if (val == null) continue;

                        // 特殊字段处理
                        switch (propName)
                        {
                            case "INC":
                                if (val is double dInc) incVal = dInc;
                                SetProperty(bed, "INC", val);   // 仍更新到视图属性
                                break;
                            case "WARM_Percent":
                                if (val is double dWarm) warmVal = dWarm;
                                SetProperty(bed, propName, val);
                                break;
                            case "W_MAN":
                                if (val is double dWman) wmanVal = dWman;
                                SetProperty(bed, propName, val);
                                break;
                            case "NIBP_SYS":
                                nibpSys = val.ToString();
                                hasNibpSys = true;
                                SetProperty(bed, propName, val);
                                break;
                            case "NIBP_DIA":
                                nibpDia = val.ToString();
                                hasNibpDia = true;
                                SetProperty(bed, propName, val);
                                break;
                            default:
                                SetProperty(bed, propName, val);
                                break;
                        }

                        // 参考范围
                        if (!string.IsNullOrEmpty(obx7))
                        {
                            var parts = obx7.Split(new[] { '-', '^' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2 && double.TryParse(parts[0], out double low) && double.TryParse(parts[1], out double high))
                            {
                                // 范围属性命名规则：原属性名 + "_High" / "_Low"
                                string baseProp = propName;
                                // 特殊处理：BloodOxygen 的范围属性是 SPO2_High / SPO2_Low
                                if (propName == "BloodOxygen") baseProp = "SPO2";
                                else if (propName == "HeartRate") baseProp = "HR";
                                else if (propName == "RespirationRate") baseProp = "RESP";

                                SetProperty(bed, baseProp + "_High", high);
                                SetProperty(bed, baseProp + "_Low", low);
                            }
                        }
                    }

                }

                // 组合 NIBP 显示字符串
                if (hasNibpSys || hasNibpDia)
                {
                    bed.BloodPressure = $"{nibpSys ?? "--"}/{nibpDia ?? "--"}";
                }

                // 3. 工作模式与百分比
                string modeChinese = "未知";
                double? workPercent = null;
                if (isKangaroo)
                {
                    modeChinese = "亲子模式";
                    workPercent = statusValue != null && statusValue.Equals("WARM", StringComparison.OrdinalIgnoreCase) ? warmVal : incVal;
                }
                else if (!string.IsNullOrEmpty(ctrlMode))
                {
                    switch (ctrlMode.ToUpper())
                    {
                        case "AIR": modeChinese = "箱温模式"; workPercent = incVal; break;
                        case "SKIN": modeChinese = "肤温模式"; workPercent = incVal; break;
                        case "MANUAL": modeChinese = "手控模式"; workPercent = wmanVal; break;
                        case "PREWARM": modeChinese = "预热模式"; workPercent = warmVal; break;
                        default: modeChinese = ctrlMode; break;
                    }
                }
                bed.WorkMode = modeChinese;
                bed.WorkModePercent = workPercent.HasValue ? (int)workPercent.Value : 0; // 转 int

                // 4. 报警字符串
                bed.WaringInform = alarmNames.Count > 0 ? string.Join("，", alarmNames) : "";
                // 5. 记录报警日志
                LogAlarms(bed, alarmCodes, alarmNames, alarmTypesForLog);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Hl7DataProcessor] 解析异常: {ex.Message}");
            }
        }

        // ========== 患者信息 ==========
        private static void ParsePatientInfo(Message message, BedViewModel bed)
        {
            var pidSeg = message.Segments("PID").FirstOrDefault();
            if (pidSeg == null) return;

            // 患者 ID
            if (int.TryParse(pidSeg.Fields(2).Value, out int patientId))
                bed.PatientId = patientId;

            // 患者姓名
            bed.PatientName = pidSeg.Fields(4).Components(1).Value ?? "--";

            // 出生日期
            string dob = pidSeg.Fields(6).Components(1).Value ?? "";
            if (dob.Length >= 8)
            {
                bed.BirthYear = dob.Substring(0, 4);
                bed.BirthMonth = dob.Substring(4, 2);
                bed.BirthDay = dob.Substring(6, 2);
            }

            // 性别
            string sex = pidSeg.Fields(7).Value;
            bed.Sex = sex == "M" ? "男" : (sex == "F" ? "女" : sex);
        }

        // ========== 波形处理 ==========
        private static bool TryExtractWaveform(string obx3, string obx5, BedViewModel bed)
        {
            string wave = CleanWave(obx5);
            if (string.IsNullOrEmpty(wave)) return false;

            if (obx3.Contains("ECG 1 lead WAVE", StringComparison.OrdinalIgnoreCase)) bed.Hr_Ⅰ = wave;
            else if (obx3.Contains("ECG 2 lead WAVE", StringComparison.OrdinalIgnoreCase)) bed.Hr_Ⅱ = wave;
            else if (obx3.Contains("ECG 3 lead WAVE", StringComparison.OrdinalIgnoreCase)) bed.Hr_Ⅲ = wave;
            else if (obx3.Contains("ECG lead aVR WAVE", StringComparison.OrdinalIgnoreCase)) bed.Hr_AVR = wave;
            else if (obx3.Contains("ECG lead aVL WAVE", StringComparison.OrdinalIgnoreCase)) bed.Hr_AVL = wave;
            else if (obx3.Contains("ECG lead aVF WAVE", StringComparison.OrdinalIgnoreCase)) bed.Hr_AVF = wave;
            else if (obx3.Contains("ECG lead V1 WAVE", StringComparison.OrdinalIgnoreCase)) bed.Hr_V1 = wave;
            else if (obx3.Contains("SPO2 WAVE", StringComparison.OrdinalIgnoreCase)) bed.PletchWaveData = wave;
            else if (obx3.Contains("CO2 WAVE", StringComparison.OrdinalIgnoreCase)) bed.Ecto2WaveData = wave;
            else if (obx3.Contains("RESP WAVE", StringComparison.OrdinalIgnoreCase)) bed.RespWaveData = wave;
            else return false;

            return true;
        }

        private static string CleanWave(string obx5)
        {
            if (string.IsNullOrEmpty(obx5)) return null;
            // 取最后一个 '^' 之后的部分
            int lastCaret = obx5.LastIndexOf('^');
            string raw = lastCaret >= 0 ? obx5.Substring(lastCaret + 1) : obx5;
            // 只保留数字、逗号、负号
            var sb = new StringBuilder();
            foreach (char c in raw)
            {
                if (char.IsDigit(c) || c == ',' || c == '-')
                    sb.Append(c);
            }
            return sb.Length > 0 ? sb.ToString() : null;
        }

        // ========== 辅助方法 ==========
        private static string ExtractParamName(string obx3)
        {
            int idx = obx3.IndexOf('^');
            return idx >= 0 ? obx3.Substring(idx + 1).Trim() : obx3.Trim();
        }

        private static object ParseNumeric(string input)
        {
            if (string.IsNullOrEmpty(input) || input == "--") return null;
            if (double.TryParse(input, out double d)) return d;
            return null;
        }

        private static void SetProperty(BedViewModel bed, string propertyName, object value)
        {
            var prop = typeof(BedViewModel).GetProperty(propertyName);
            if (prop != null && value != null)
            {
                // 统一转换为 string 赋值（因为大部分 BedViewModel 属性是 string，WorkModePercent 是 int 会特殊处理）
                if (prop.PropertyType == typeof(int))
                    prop.SetValue(bed, Convert.ToInt32(value));
                else
                    prop.SetValue(bed, value.ToString());
            }
        }
    }
}
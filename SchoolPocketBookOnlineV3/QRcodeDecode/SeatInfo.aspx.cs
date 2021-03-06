﻿using System;
using System.Collections.Generic;
using System.Text;
using SeatManage.ClassModel;
using SeatManage.EnumType;
using SeatManage.IPocketBespeakBllServiceV2;
using SeatManage.PocketBespeakBllServiceV2;
using SeatManage.SeatManageComm;
using ScanCodeParamModel = SchoolPocketBookWeb.Code.ScanCodeParamModel;

namespace SchoolPocketBookWeb.QRcodeDecode
{
    public partial class SeatInfo : BasePage
    {
        public string cmd;
        public string state;
        public string bookNo;
        public string cardNo;
        public string seatNo;
        public string readingRoomNo;
        public string listMessage = "";
        SeatBookUsingInfo scmodel;
        private IPocketBespeakBllService handler = new PocketBespeakBllService();
        //private SeatManage.IPocketBespeak.IMainFunctionPageBll handler = new SeatManage.PocketBespeak.PocketBespeak_MainFunctionPageBll();
        //private SeatManage.IPocketBespeak.IQueryLogs handler1 = new SeatManage.PocketBespeak.PocketBespeak_QueryLogs();
        //private SeatManage.IPocketBespeak.IWaitSeat handler2 = new SeatManage.PocketBespeak.PocketBespeak_WaitSeat();
        //private SeatManage.IPocketBespeak.IBespeakSeatListForm handler3 = new SeatManage.PocketBespeak.PocketBespeak_BespeakSeat();
        protected void Page_Load(object sender, EventArgs e)
        {
            string strparam = Request.QueryString["param"];
            if (string.IsNullOrEmpty(strparam))
            {
                spanWarmInfo.InnerText = "非正常的访问！";
                divHanderPanel.Style.Add("display", "none");
                return;
            }

            ScanCodeParamModel param = new ScanCodeParamModel(strparam);
            seatNo = param.SeatNum;
            readingRoomNo = param.ReadingRoomNum;
            if (LoginUserInfo != null)//存在记录的cookies信息
            {
                cardNo = LoginUserInfo.CardNo;
            }
            else
            {
                string url = Request.Url.AbsoluteUri;
                //string url = "/QRcodeDecode/SeatInfo.aspx?param=" + strparam;
                Response.Redirect(LoginUrl() + "?url=" + url);
            }
            //UserInfo user = SeatManage.Bll.Users_ALL.GetUserInfo(cardNo);
            //SeatManage.ClassModel.ManagerPotency potency = SeatManage.Bll.T_SM_ManagerPotency.GetManangePotencyByLoginID(cardNo);
            //if (user == null || user.UserType != SeatManage.EnumType.UserType.Admin || potency.RightRoomList.All(u => u.No != readingRoomNo))
            //{
            //    spanWarmInfo.InnerText = "您没有权限！";
            //    divHanderPanel.Style.Add("display", "none");
            //    return;
            //}



            if (!IsPostBack)
            {
                //DataBind(cardNo, param.SeatNum, param.ReadingRoomNum);
                ShowReaderState();
            }
            else
            {
                string cmd = Request.Form["subCmd"];
                string result;
                switch (cmd)
                {
                    case "changeSeat":
                        result = handler.ChangeSeat( cardNo, param.SeatNum, param.ReadingRoomNum);
                        if (!string.IsNullOrEmpty(result))
                        {
                            spanWarmInfo.InnerText = result;
                        }
                        else
                        {
                            spanWarmInfo.InnerText = "更换座位成功";
                            //DataBind(cardNo, param.SeatNum, param.ReadingRoomNum);
                            ShowReaderState();
                            //this.divHanderPanel.Style.Add("display", "none"); 
                        }
                        break;
                    case "selectSeat":
                        result = handler.SelectSeat( cardNo, param.SeatNum, param.ReadingRoomNum);
                        if (!string.IsNullOrEmpty(result))
                        {
                            spanWarmInfo.InnerText = result;
                        }
                        else
                        {
                            spanWarmInfo.InnerText = "选择座位成功";
                            //DataBind(cardNo, param.SeatNum, param.ReadingRoomNum);
                            ShowReaderState();
                            //this.divHanderPanel.Style.Add("display", "none"); 
                        }
                        break;
                    case "waitSeat":
                        if (!handler.IsCanWaitSeat(LoginUserInfo.CardNo, readingRoomNo))
                        {
                            spanWarmInfo.Visible = true;
                            spanWarmInfo.InnerText = "您等待座位的间隔过短，请稍后重试。";
                        }
                        else
                        {
                            WaitSeatLogInfo waitInfo = new WaitSeatLogInfo();
                            waitInfo.CardNo = LoginUserInfo.CardNo;
                            waitInfo.SeatNo = seatNo;
                            waitInfo.NowState = LogStatus.Valid;
                            waitInfo.OperateType = Operation.Reader;
                            waitInfo.WaitingState = EnterOutLogType.Waiting;
                            result = handler.SubmitWaitInfo( waitInfo);
                            if (!string.IsNullOrEmpty(result))
                            {
                                spanWarmInfo.InnerText = result;
                            }
                            else
                            {
                                spanWarmInfo.InnerText = "等待座位成功";
                            }
                        }
                        ShowReaderState();
                        break;
                    case "shortLeave":
                        shortLeaveHandle();//设置读者暂离
                        //this.LoginUserInfo = handler.GetReaderInfo(this.UserSchoolInfo, this.LoginUserInfo.CardNo);//重新绑定读者状态
                        ShowReaderState();
                        break;
                    case "leave":
                        //释放读者座位
                        freeSeat();
                        //this.LoginUserInfo = handler.GetReaderInfo(this.UserSchoolInfo, this.LoginUserInfo.CardNo);
                        ShowReaderState();
                        break;
                    case "LoginOut":
                        Session.Clear();
                        Response.Cookies["userInfo"].Expires = DateTime.Now.AddDays(-1);
                        CookiesManager.RefreshNum = 0;
                        Response.Redirect(LogoutUrl());
                        break;
                    case "ContinuedWhen":
                        LoginUserInfo = handler.GetReaderInfo(LoginUserInfo.CardNo);
                        if (LoginUserInfo.EnterOutLog != null && LoginUserInfo.EnterOutLog.EnterOutState != EnterOutLogType.Leave)
                        {
                            switch (LoginUserInfo.EnterOutLog.EnterOutState)
                            {
                                case EnterOutLogType.BookingConfirmation:
                                case EnterOutLogType.SelectSeat:
                                case EnterOutLogType.ContinuedTime:
                                case EnterOutLogType.ComeBack:
                                case EnterOutLogType.ReselectSeat:
                                case EnterOutLogType.WaitingSuccess:
                                    LoginUserInfo.EnterOutLog.Remark = "通过手机预约网站延长座位使用时间";
                                    LoginUserInfo.EnterOutLog.EnterOutState = EnterOutLogType.ContinuedTime;
                                    ContinuedWhen();
                                    ShowReaderState();
                                    break;
                                case EnterOutLogType.ShortLeave:
                                    spanWarmInfo.Visible = true;
                                    spanWarmInfo.InnerText = "续时失败，你处于暂离状态";
                                    break;
                            }
                        }
                        else
                        {
                            spanWarmInfo.Visible = true;
                            spanWarmInfo.InnerText = "续时失败，您还没有选座";
                        }
                        break;
                    case "ComeBack":
                        LoginUserInfo = handler.GetReaderInfo( LoginUserInfo.CardNo);
                        if (LoginUserInfo.EnterOutLog != null && LoginUserInfo.EnterOutLog.EnterOutState == EnterOutLogType.ShortLeave)
                        {
                            LoginUserInfo.EnterOutLog.Remark = "通过手机预约网站恢复在座";
                            LoginUserInfo.EnterOutLog.EnterOutState = EnterOutLogType.ComeBack;
                            ComeBack();
                            ShowReaderState();
                        }
                        spanWarmInfo.Visible = true;
                        spanWarmInfo.InnerText = "暂离回来失败，您还没有暂离";
                        break;
                    case "cancel":
                        CancelBookLog(bookNo);
                        confrimSeat();
                        //this.LoginUserInfo = handler.GetReaderInfo(this.UserSchoolInfo, this.LoginUserInfo.CardNo);//重新绑定读者状态
                        ShowReaderState();
                        break;
                    case "CancelWait":
                        if (LoginUserInfo.WaitSeatLog != null)
                        {
                            spanWarmInfo.Visible = true;
                            spanWarmInfo.InnerText = handler.CancelWait( LoginUserInfo.WaitSeatLog);
                            ShowReaderState();
                        }
                        else
                        {
                            spanWarmInfo.Visible = true;
                            spanWarmInfo.InnerText = "当前没有等待的座位";
                        }
                        break;
                    case "CancelBook":
                        if (LoginUserInfo.BespeakLog != null && LoginUserInfo.BespeakLog.Count > 0)
                        {
                            spanWarmInfo.Visible = true;
                            if (handler.UpdateBookLogsState(int.Parse(LoginUserInfo.BespeakLog[0].BsepeaklogID)))
                            {
                                spanWarmInfo.InnerText = "取消预约成功";
                                //confrimSeat();
                                //this.LoginUserInfo = handler.GetReaderInfo(this.UserSchoolInfo, this.LoginUserInfo.CardNo);//重新绑定读者状态
                                ShowReaderState();
                            }
                            else
                            {
                                spanWarmInfo.InnerText = "取消预约取消失败";
                            }
                        }
                        else
                        {
                            spanWarmInfo.Visible = true;
                            spanWarmInfo.InnerText = "当前没有预约的座位";
                        }
                        break;
                    case "BookConfirm":
                        if (LoginUserInfo.BespeakLog != null && LoginUserInfo.BespeakLog.Count > 0)
                        {
                            confrimSeat();
                            //this.LoginUserInfo = handler.GetReaderInfo(this.UserSchoolInfo, this.LoginUserInfo.CardNo);//重新绑定读者状态
                            ShowReaderState();
                        }
                        else
                        {
                            spanWarmInfo.Visible = true;
                            spanWarmInfo.InnerText = "当前没有预约的座位";
                        }
                        break;

                }
                subCmd.Value = "";
            }
        }
        /// <summary>
        /// 获取座位信息
        /// </summary>
        /// <param name="cardNo"></param>
        /// <param name="seatNum"></param>
        /// <param name="readingRoomNum"></param>
        void DataBind()
        {
            scmodel = handler.GetSeatBookUsingStatus(seatNo,readingRoomNo);
            if (scmodel != null && scmodel.SeatInfo != null)
            {
                seatlblReadingRoomName.InnerText = scmodel.SeatInfo.ReadingRoom.Name;
                seatlblSeatNo.InnerText = scmodel.SeatInfo.ShortSeatNo;
                switch (scmodel.SeatInfo.SeatUsedState)
                {
                    case EnterOutLogType.ComeBack:
                    case EnterOutLogType.ContinuedTime:
                    case EnterOutLogType.ReselectSeat:
                    case EnterOutLogType.ShortLeave:
                    case EnterOutLogType.WaitingSuccess:
                    case EnterOutLogType.SelectSeat:
                    case EnterOutLogType.BookingConfirmation:
                    case EnterOutLogType.BespeakWaiting:
                        seatlblSeatStatus.InnerText = "正在使用中";
                        break;
                    default:
                        seatlblSeatStatus.InnerText = "空闲";
                        break;
                }

                StringBuilder sbListInfo = new StringBuilder();
                sbListInfo.Append("<li data-theme='d' data-role='list-divider' role='heading'>座位预约 </li>");
                if (scmodel.SeatInfo.IsSuspended)
                {
                    seatlblSeatStatus.InnerText = "已被停用";
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = "此座位已被停用";
                    return;
                }
                if (!scmodel.InReadingRoom.Setting.SeatBespeak.Used)
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = "此座位不提供预约";
                    return;
                }
                if (scmodel.BookSeatInfo.Count < 1)
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = "此座位没有可预约的时间段";
                    return;
                }

                foreach (KeyValuePair<DateTime, Seat> item in scmodel.BookSeatInfo)
                {
                    if (item.Key.Date.CompareTo(DateTime.Now.Date) == 0)
                    {
                        sbListInfo.Append(string.Format("<li date-theme='d'>{0}<ul date-theme='d'><input data-inline='true' data-mini='false' value='预约' type='button'onclick=\"location.href='../BookSeat/BookNowSeatMessage.aspx?seatNo=\"{1}\"&seatShortNo=\"{2}\"&roomNo=\"{3}\"&date=\"{4}\"&timeSpan=\"{5}'\")' /></li>", item.Key.ToLongDateString(), item.Value.SeatNo, item.Value.ShortSeatNo, item.Value.ReadingRoomNum, item.Key.ToLongDateString(), item.Value.CanBespeakStr));

                    }
                    else
                    {
                        sbListInfo.Append(string.Format("<li date-theme='d'>{0}<ul date-theme='d'><input data-inline='true' data-mini='false' value='预约' type='button'onclick=\"location.href='../BookSeat/BookSeatMessage.aspx?seatNo=\"{1}\"&seatShortNo=\"{2}\"&roomNo=\"{3}\"&date=\"{4}\"&timeSpan=\"{5}'\")' /></li>", item.Key.ToLongDateString(), item.Value.SeatNo, item.Value.ShortSeatNo, item.Value.ReadingRoomNum, item.Key.ToLongDateString(), item.Value.CanBespeakStr));
                    }
                    //sbListInfo.Append(string.Format("<li><input data-inline='true' data-mini='false' value='预约' type='button'onclick=\"location.href='../MainFunctionPage.aspx'\")' /></li>", bookLogList[i].BsepeaklogID));
                }
                sbListInfo.Append("</ul></li>");

                listMessage = sbListInfo.ToString();
            }
        }

        private void ShowReaderState()
        {
            scmodel = handler.GetSeatBookUsingStatus(seatNo, readingRoomNo);
            bool isCanUseSeat = false;
            if (scmodel != null && scmodel.SeatInfo != null)
            {
                seatlblReadingRoomName.InnerText = scmodel.SeatInfo.ReadingRoom.Name;
                seatlblSeatNo.InnerText = scmodel.SeatInfo.ShortSeatNo;

                switch (scmodel.SeatInfo.SeatUsedState)
                {
                    case EnterOutLogType.ComeBack:
                    case EnterOutLogType.ContinuedTime:
                    case EnterOutLogType.ReselectSeat:
                    case EnterOutLogType.ShortLeave:
                    case EnterOutLogType.WaitingSuccess:
                    case EnterOutLogType.SelectSeat:
                    case EnterOutLogType.BookingConfirmation:
                    case EnterOutLogType.BespeakWaiting:
                        seatlblSeatStatus.InnerText = "正在使用中";
                        break;
                    default:
                        seatlblSeatStatus.InnerText = "空闲";
                        isCanUseSeat = true;
                        break;
                }
                if (scmodel.SeatInfo.IsSuspended)
                {
                    seatlblSeatStatus.InnerText = "已被停用";
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = "此座位已被停用";
                }
                else if (!scmodel.InReadingRoom.Setting.SeatBespeak.Used)
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = "此座位不提供预约";
                }
                else if (scmodel.BookSeatInfo.Count < 1)
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = "此座位没有可预约的时间段";
                }
                else
                {
                    List<ReadingRoomInfo> roomList = handler.GetAllReadingRoomInfo();
                    if (ReadingRoomList == null)
                    {
                        ReadingRoomList = new Dictionary<string, ReadingRoomInfo>();
                    }
                    else
                    {
                        ReadingRoomList.Clear();
                    }
                    foreach (ReadingRoomInfo item in roomList)
                    {
                        ReadingRoomList.Add(item.No, item);
                    }

                    StringBuilder sbListInfo = new StringBuilder();
                    sbListInfo.Append("<li data-theme='d' data-role='list-divider' role='heading'>座位预约 </li>");
                    foreach (KeyValuePair<DateTime, Seat> item in scmodel.BookSeatInfo)
                    {
                        if (item.Key.Date.CompareTo(DateTime.Now.Date) == 0)
                        {
                            sbListInfo.Append(string.Format("<li date-theme='d' style=\"padding-top: 0px;padding-bottom: 0px;\">{0}&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<input data-inline='true' data-mini='true' value='预约' type='button'onclick=\"location.href='../BookSeat/BookNowSeatMessage.aspx?seatNo={1}&seatShortNo={2}&roomNo={3}&date={4}&timeSpan={5}'\")' /></li>", item.Key.ToLongDateString(), item.Value.SeatNo, item.Value.ShortSeatNo, readingRoomNo, item.Key.ToLongDateString(), item.Value.CanBespeakStr));

                        }
                        else
                        {
                            sbListInfo.Append(string.Format("<li date-theme='d' style=\"padding-top: 0px;padding-bottom: 0px;\">{0}&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<input data-inline='true' data-mini='true' value='预约' type='button'onclick=\"location.href='../BookSeat/BookSeatMessage.aspx?seatNo={1}&seatShortNo={2}&roomNo={3}&date={4}&timeSpan={5}'\")' /></li>", item.Key.ToLongDateString(), item.Value.SeatNo, item.Value.ShortSeatNo, readingRoomNo, item.Key.ToLongDateString(), item.Value.CanBespeakStr));
                        }
                        //sbListInfo.Append(string.Format("<li><input data-inline='true' data-mini='false' value='预约' type='button'onclick=\"location.href='../MainFunctionPage.aspx'\")' /></li>", bookLogList[i].BsepeaklogID));
                    }
                    sbListInfo.Append("</li>");

                    listMessage = sbListInfo.ToString();
                }
            }

            LoginUserInfo = handler.GetReaderInfo( LoginUserInfo.CardNo);
            ReaderInfo reader = LoginUserInfo;
            if (reader.EnterOutLog == null)
            {
                state = "Leave";
            }
            else
            {
                state = reader.EnterOutLog.EnterOutState.ToString();
            }

            if (reader.BespeakLog.Count > 0 && state == "Leave")
            {
                state = "Booking";
            }
            if (reader.WaitSeatLog != null)
            {
                state = "Waiting";
            }
            btnLeave.Visible = false;
            btnShortLeave.Visible = false;
            btn_ComeBack.Visible = false;
            btn_ContinuedWhen.Visible = false;
            btn_CancelBook.Visible = false;
            btn_CancelWait.Visible = false;
            btn_BookConfirm.Visible = false;
            btn_SelectSeat.Visible = false;
            btn_ChangeSeat.Visible = false;
            btn_WaitSeat.Visible = false;

            lblReadingRoomName.InnerText = "无";
            lblSeatNo.InnerText = "无";
            lblSeatStatus.InnerText = "无";
            lblenterOutTime.InnerText = "无";
            lblRemark.InnerText = "无";
            switch (state)
            {
                case "SelectSeat":
                case "ComeBack":
                case "ContinuedTime":
                case "WaitingSuccess":
                case "BookingConfirmation":
                case "ReselectSeat":
                    lblSeatStatus.InnerText = "在座";
                    lblReadingRoomName.InnerText = reader.EnterOutLog.ReadingRoomName;
                    lblSeatNo.InnerText = reader.EnterOutLog.ShortSeatNo;
                    lblenterOutTime.InnerText = reader.EnterOutLog.EnterOutTime.ToLongTimeString();
                    lblRemark.InnerText = reader.EnterOutLog.Remark;
                    if (reader.EnterOutLog.SeatNo == seatNo && readingRoomNo == reader.EnterOutLog.ReadingRoomNo)
                    {
                        if (reader.PecketWebSetting.UseShortLeave)
                        {
                            btnShortLeave.Visible = true;
                        }
                        if (reader.PecketWebSetting.UseCanLeave)
                        {
                            btnLeave.Visible = true;
                        }
                        if (reader.PecketWebSetting.UseContinue && reader.AtReadingRoom.Setting.SeatUsedTimeLimit.Used && reader.AtReadingRoom.Setting.SeatUsedTimeLimit.IsCanContinuedTime)
                        {
                            btn_ContinuedWhen.Visible = true;
                        }
                    }
                    else if (isCanUseSeat)
                    {
                        if (reader.PecketWebSetting.UseChangeSeat)
                        {
                            btn_ChangeSeat.Visible = true;
                        }
                    }
                    break;
                case "Leave":
                    if (isCanUseSeat)
                    {
                        if (reader.PecketWebSetting.UseSelectSeat)
                        {
                            btn_SelectSeat.Visible = true;
                        }
                    }
                    else if (scmodel.SeatInfo.SeatUsedState != EnterOutLogType.ShortLeave)
                    {
                        if (reader.PecketWebSetting.UseWaitSeat)
                        {
                            btn_WaitSeat.Visible = true;
                        }
                    }
                    break;
                case "Booking":
                    lblSeatStatus.InnerText = "预约等待签到中";
                    lblReadingRoomName.InnerText = reader.BespeakLog[0].ReadingRoomName;
                    lblSeatNo.InnerText = reader.BespeakLog[0].ShortSeatNum;
                    lblenterOutTime.InnerText = reader.BespeakLog[0].BsepeakTime.ToLongTimeString();
                    lblRemark.InnerText = reader.BespeakLog[0].Remark;
                    if (reader.BespeakLog[0].SeatNo == seatNo && readingRoomNo == reader.BespeakLog[0].ReadingRoomNo)
                    {
                        if (reader.PecketWebSetting.UseCancelBook)
                        {
                            btn_CancelBook.Visible = true;
                        }
                        if (reader.PecketWebSetting.UseBookComfirm)
                        {
                            if (reader.BespeakLog[0].SubmitTime == reader.BespeakLog[0].BsepeakTime)
                            {
                                btn_BookConfirm.Visible = true;
                            }
                            else
                            {
                                if (reader.BespeakLog[0].BsepeakTime.AddMinutes(-double.Parse(reader.AtReadingRoom.Setting.SeatBespeak.ConfirmTime.BeginTime)) <= DateTime.Now)
                                {
                                    btn_BookConfirm.Visible = true;
                                }
                            }
                        }
                    }
                    break;
                case "Waiting":
                    lblSeatStatus.InnerText = "等待座位";
                    lblReadingRoomName.InnerText = reader.WaitSeatLog.EnterOutLog.ReadingRoomName;
                    lblSeatNo.InnerText = reader.WaitSeatLog.EnterOutLog.ShortSeatNo;
                    lblenterOutTime.InnerText = reader.WaitSeatLog.SeatWaitTime.ToLongTimeString();
                    lblRemark.InnerText = "您把读者" + reader.WaitSeatLog.EnterOutLog.CardNo + "设置为暂离，并等待此座位。";
                    if (reader.WaitSeatLog.EnterOutLog.SeatNo == seatNo && readingRoomNo == reader.WaitSeatLog.EnterOutLog.ReadingRoomNo)
                    {
                        if (reader.PecketWebSetting.UseCancelWait)
                        {
                            btn_CancelWait.Visible = true;
                        }
                    }
                    break;
                case "ShortLeave":
                    lblSeatStatus.InnerText = "暂离";
                    lblReadingRoomName.InnerText = reader.EnterOutLog.ReadingRoomName;
                    lblSeatNo.InnerText = reader.EnterOutLog.ShortSeatNo;
                    lblenterOutTime.InnerText = reader.EnterOutLog.EnterOutTime.ToLongTimeString();
                    lblRemark.InnerText = reader.EnterOutLog.Remark;
                    if (reader.EnterOutLog.SeatNo == seatNo && readingRoomNo == reader.EnterOutLog.ReadingRoomNo)
                    {
                        if (reader.PecketWebSetting.UseComeBack)
                        {
                            btn_ComeBack.Visible = true;
                        }
                        if (reader.PecketWebSetting.UseCanLeave)
                        {
                            btnLeave.Visible = true;
                        }
                    }
                    else if (isCanUseSeat)
                    {
                        if (reader.PecketWebSetting.UseChangeSeat)
                        {
                            btn_ChangeSeat.Visible = true;
                        }
                    }
                    break;
                default: lblSeatStatus.InnerText = "没有座位";
                    if (isCanUseSeat)
                    {
                        if (reader.PecketWebSetting.UseSelectSeat)
                        {
                            btn_ChangeSeat.Visible = true;
                        }
                    }
                    else if (scmodel.SeatInfo.SeatUsedState != EnterOutLogType.ShortLeave)
                    {
                        if (reader.PecketWebSetting.UseWaitSeat)
                        {
                            btn_WaitSeat.Visible = true;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 绑定预约记录信息
        /// </summary>
        /// <param name="cardNo">学号</param>
        /// <param name="rrId">阅览室编号</param>
        /// <param name="queryDate">查询日期</param>
        private void ShowBookSeats(Dictionary<DateTime, Seat> seatDic)
        {
            try
            {

            }

            catch (Exception ex)
            {
                listMessage = "查询出错" + ex.Message;
            }
        }

        private void confrimSeat()
        {
            try
            {
                string resultValue = handler.CheckSeat( int.Parse(LoginUserInfo.BespeakLog[0].BsepeaklogID));
                if (!string.IsNullOrEmpty(resultValue))
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = resultValue;
                }
            }
            catch (Exception ex)
            {
                spanWarmInfo.Visible = true;
                spanWarmInfo.InnerText = ex.Message;

            }
        }
        /// <summary>
        /// 设置读者暂离的业务逻辑
        /// </summary>
        /// <returns></returns>
        private void shortLeaveHandle()
        {
            try
            {
                string resultValue = handler.SetShortLeave(LoginUserInfo.CardNo);
                if (!string.IsNullOrEmpty(resultValue))
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = resultValue;
                }
            }
            catch (Exception ex)
            {
                spanWarmInfo.Visible = true;
                spanWarmInfo.InnerText = ex.Message;

            }
        }

        /// <summary>
        /// 释放座位
        /// </summary>
        private void freeSeat()
        {
            try
            {
                string resultValue = handler.FreeSeat( LoginUserInfo.CardNo);
                if (!string.IsNullOrEmpty(resultValue))
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = resultValue;
                }
            }
            catch (Exception ex)
            {
                spanWarmInfo.Visible = true;
                spanWarmInfo.InnerText = ex.Message;

            }
        }
        /// <summary>
        /// 续时
        /// </summary>
        private void ContinuedWhen()
        {
            try
            {
                string resultValue = handler.DelaySeatUsedTime(LoginUserInfo);
                if (!string.IsNullOrEmpty(resultValue))
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = resultValue;
                }
                else
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = "操作成功";
                }
            }
            catch (Exception ex)
            {
                spanWarmInfo.Visible = true;
                spanWarmInfo.InnerText = ex.Message;

            }
        }
        /// <summary>
        /// 暂离回来
        /// </summary>
        private void ComeBack()
        {
            try
            {
                string resultValue = handler.ReaderComeBack(LoginUserInfo);
                if (!string.IsNullOrEmpty(resultValue))
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = resultValue;
                }
                else
                {
                    spanWarmInfo.Visible = true;
                    spanWarmInfo.InnerText = "操作成功";
                }
            }
            catch (Exception ex)
            {
                spanWarmInfo.Visible = true;
                spanWarmInfo.InnerText = ex.Message;

            }
        }
        /// <summary>
        /// 取消预约记录
        /// </summary>
        /// <param name="bookNo"></param>
        /// <param name="bookCancelPerson"></param>
        /// <param name="conn"></param>
        private void CancelBookLog(string bookNo)
        {
            try
            {
                bool result = handler.UpdateBookLogsState(int.Parse(bookNo));
                if (result)
                {
                    ClientScript.RegisterStartupScript(GetType(), "closewindow", "alert('成功取消预约！');window.close();", true);
                }
                else
                {
                    ClientScript.RegisterStartupScript(GetType(), "closewindow", "alert('取消预约失败！');window.close();", true);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
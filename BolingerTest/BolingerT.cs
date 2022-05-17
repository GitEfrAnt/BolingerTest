using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptSolution;
using ScriptSolution.Indicators;
using ScriptSolution.Model;
using ScriptSolution.Model.Interfaces;
using SourceEts.Config;

namespace BolingerTest
{
    public class BolingerT: Script
    {
        public CreateInidicator BbOpen = new CreateInidicator(EnumIndicators.BollinderBands, 0, "1111Полосы боллинджера для входа в позицию");
        public CreateInidicator BbClose = new CreateInidicator(EnumIndicators.BollinderBands, 0,"Полосы боллинджера для выхода из позиции");

        public ParamOptimization Volume1 = new ParamOptimization(1, 1, 1000, 1, "Объем первой позиции контрактов", "Объем первой позиции контрактов.");
        public ParamOptimization Volume2 = new ParamOptimization(1, 1, 1000, 1, "Объем второй позиции контрактов", "Объем второй позиции контрактов.");

        public ParamOptimization TakeProfit = new ParamOptimization(1000, 50, 10000, 50, "Профит для первой позиции", "Профит для первой позиции в шагах цены");

        public ParamOptimization OpenShift = new ParamOptimization(1, 1, 1000, 1, "Отступ Открытие",
            "Отступ Открытие (в шагах цены) - величина, на которую должен произойти пробой полосы боллинджера.");
        public ParamOptimization CloseShift = new ParamOptimization(1, 1, 1000, 1, "Отступ Закрытие ",
            "Отступ Открытие (в шагах цены) - величина, на которую должен произойти пробой полосы боллинджера.");


        // private string _dir { get; set; } = " ";
        IPosition Long1 = null;
        IPosition Long2 = null;
        IPosition Short1 = null;
        IPosition Short2 = null;

        public override void Execute()
        {

           
            for (var bar = IndexBar; bar < CandleCount - 1; bar++)
            {
                if (BbOpen.param.LinesIndicators[0].LineParam[0].Value < bar)
                {

                    var CloseLine = BbClose.param.LinesIndicators[1].PriceSeries;

                    var OpenLine = BbOpen.param.LinesIndicators[2].PriceSeries;

                    bool Exit1 = false;

                    //Закрытие длинной позиции
                    if (LongPos.Count > 0)

                    {
                        if (Long2 != null)
                        {
                            if (Candles.CloseSeries[bar] > CloseLine[bar])// или (+EntryBreakOut) не понятно из условия нужен  отступ при закрытии второй позиции
                            {
                                SellAtMarket(bar + 1, Long2, "Пересечение верхней линии. 2-я позиция");
                                //Exit2 = true;
                                Long2 = null;

                            }
                        }

                        double ExitBreakOut = (CloseLine[bar] + CloseShift.ValueInt * FinInfo.Security.MinStep);
                        if (Long1 != null)
                        {
                            double Profit = Long1.EntryPrice + TakeProfit.Value * FinInfo.Security.MinStep;
                            if (Profit < ExitBreakOut)
                                if (Candles.CloseSeries[bar] >= Profit)
                                {
                                    this.AddLogRobot(bar.ToString() + " " + "EntryPrice= " + Long1.EntryPrice + " " + "ProfitPrice= " + Profit + " " + (Long1 == null).ToString() + " " + LongPos.Count.ToString());
                                    SellAtProfit(bar + 1, Long1, Profit, "Profit");
                                    Long1 = null;
                                    Exit1 = true;
                                    this.AddLogRobot("Profit " + (Long1 == null).ToString() + " " + LongPos.Count.ToString());
                                }

                        }

                        if (Long1 != null)
                        {

                            if (Candles.CloseSeries[bar] > ExitBreakOut)
                            {
                                {
                                    this.AddLogRobot("Закрытие " + bar + " " + (Long1 == null).ToString() + " " + LongPos.Count.ToString());
                                    SellAtMarket(bar + 1, Long1, "Пересечение верхней линии. 1-я позиция.");
                                    Long1 = null;
                                    this.AddLogRobot((Long1 == null).ToString() + " " + LongPos.Count.ToString());

                                }
                            }
                        }






                    }


                    double EntryBreakOutPrice = OpenLine[bar] - OpenShift.ValueInt * FinInfo.Security.MinStep;
                    if (ShortPos.Count == 0)
                        if (Candles.CloseSeries[bar] <= EntryBreakOutPrice & (!Exit1))
                        {
                            if (Long1 == null)//или так(LongPos.Count==0) если нужно дождаться закрытия второй до открытия первой
                            {
                                string comment = Long2 != null ? "1-я повторная" : "1-я Пересечение нижней линии.";
                                Long1 = BuyAtMarket(bar + 1, Volume1.ValueInt, comment);

                            }

                            if (Long2 == null)// & (!Exit2))
                            {
                                Long2 = BuyAtMarket(bar + 1, Volume2.ValueInt, "2-я Пересечение нижней линии.");
                            }
                        }





                    CloseLine = BbClose.param.LinesIndicators[2].PriceSeries;

                    OpenLine = BbOpen.param.LinesIndicators[1].PriceSeries;

                    Exit1 = false;

                    //Закрытие короткой позиции
                    if (ShortPos.Count > 0)

                    {
                        if (Short2 != null)
                        {
                            if (Candles.CloseSeries[bar] < CloseLine[bar])// или (-EntryBreakOut) не понятно из условия нужен ли здесь отступ
                            {
                                CoverAtMarket(bar + 1, Short2, "Пересечение нижней линии. 2-я позиция");
                                //Exit2 = true;
                                Short2 = null;

                            }
                        }

                        double ExitBreakOut = (CloseLine[bar] - CloseShift.ValueInt * FinInfo.Security.MinStep);
                        if (Short1 != null)
                        {
                            double Profit = Short1.EntryPrice - TakeProfit.Value * FinInfo.Security.MinStep;
                            if (Profit < ExitBreakOut)
                                if (Candles.CloseSeries[bar] <= Profit)
                                {
                                    //SellAtMarket(bar + 1, Short1, "По профиту." + bar.ToString());// {bar} , {Short1.ExitOrderStatus}");
                                    int sc = ShortPos.Count;
                                    this.AddLogRobot(bar.ToString() + " " + "EntryPrice= " + Short1.EntryPrice + " " + "ProfitPrice= " + Profit + " " + (Short1 == null).ToString() + " " + ShortPos.Count.ToString());
                                    CoverAtProfit(bar + 1, Short1, Profit, "Profit");
                                    Short1 = null;
                                    Exit1 = true;
                                    this.AddLogRobot("Profit " + (Short1 == null).ToString() + " " + ShortPos.Count.ToString());
                                }

                        }

                        if (Short1 != null)
                        {

                            if (Candles.CloseSeries[bar] < ExitBreakOut)
                            {
                                {
                                    this.AddLogRobot("Закрытие " + bar + " " + (Short1 == null).ToString() + " " + LongPos.Count.ToString());
                                    CoverAtMarket(bar + 1, Short1, "Пересечение нижней линии. 1-я позиция.");
                                    Short1 = null;
                                    this.AddLogRobot((Short1 == null).ToString() + " " + LongPos.Count.ToString());

                                }
                            }
                        }






                    }


                    EntryBreakOutPrice = OpenLine[bar] - OpenShift.ValueInt * FinInfo.Security.MinStep;
                    if (LongPos.Count == 0)
                        if (Candles.CloseSeries[bar] >= EntryBreakOutPrice & (!Exit1))
                        {
                            if (Short1 == null)//или так(ShortPos.Count==0) если нужно дождаться закрытия второй до открытия первой
                            {
                                string comment = Short2 != null ? "1-я повторная" : "1-я Пересечение верхней линии.";
                                Short1 = ShortAtMarket(bar + 1, Volume1.ValueInt, comment);

                            }

                            if (Short2 == null)
                            {
                                Short2 = ShortAtMarket(bar + 1, Volume2.ValueInt, "2-я Пересечение верхней линии.");
                            }
                        }








                }
            }
        }
        public override void SetSettingDefault()
        {

        }

        
        public override void GetAttributesStratetgy()
        {
            DesParamStratetgy.Version = "1.0.0.0";
            DesParamStratetgy.DateRelease = "16.05.2022";
            DesParamStratetgy.DateChange = "16.05.2022";
            DesParamStratetgy.Author = "Efrant";
            DesParamStratetgy.Description = "Открытие позиции двумя частями. Открытие лонг(две части) происходит при пересечении ценой нижней линии боллинжера(берется значение последней закрытой свечи) для входа, цена входа = нижняя линия боллинжера -отступ на открытие. Закрытие первой части происходит при достижении профита или при пересечении верхней линии боллинжера(берется значение последней закрытой свечи) на выход на заданный отступ закрытие. Вторая часть позиции только при пересечении верхней линии боллинжера(берется значение последней закрытой свечи) для выхода из позиции."+
                                            "Если первая часть позиции была закрыта, то ее повторное и последующее открытие возможно при повторном пробитии нижней полосы боллинжера(берется значение последней закрытой свечи) минус отступ на открытие."+
                                            "Для Шорт зеркальная ситуация."+
            "Открытие позиции двумя частями. Открытие лонг(две части) происходит при пересечении ценой нижней линии боллинжера(берется значение последней закрытой свечи) для входа, цена входа = нижняя линия боллинжера -отступ на открытие. Закрытие первой части происходит при достижении профита или при пересечении верхней линии боллинжера(берется значение последней закрытой свечи) на выход на заданный отступ закрытие. Вторая часть позиции только при пересечении верхней линии боллинжера(берется значение последней закрытой свечи) для выхода из позиции." +
            "Если первая часть позиции была закрыта, то ее повторное и последующее открытие возможно при повторном пробитии нижней полосы боллинжера(берется значение последней закрытой свечи) минус отступ на открытие." +
            "Для Шорт зеркальная ситуация.";

            DesParamStratetgy.Change = "";
            DesParamStratetgy.NameStrategy = "Тест Ефремов";
            
        }
        
      


    }

}

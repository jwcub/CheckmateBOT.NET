﻿using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Chrome;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace CheckmateBOT.NET
{
    class CheckmateBOT
    {
        /*
         * https://kana.byha.top:444/post/6735
         * https://www.luogu.com.cn/paste/nbyi7ds9
         */

        private string kanaLink;
        private ChromeDriver driver;
        private string username;
        private string password;
        private string roomId;
        private bool isSecret;
        private bool isAutoReady;
        private int[,] mpType;
        private int[,] mpTmp;
        private int[,] mpBelong;
        private bool[,] tmpVis;

        private bool[,] vis;
        private List<int[]> q = new List<int[]>();
        public bool error;
        private int sx;
        private int sy;
        private int size;
        private List<int[]> homes = new List<int[]>();
        private List<int[]> tmpQ = new List<int[]>();
        private List<int[]> route = new List<int[]>();
        private bool endTag;
        private int ansLen;

        private int userCount;
        private Random rd = new Random();

        private int[,] di = { { -1, 0 }, { 0, 1 }, { 1, 0 }, { 0, -1 } };
        private IWebElement table;

        public CheckmateBOT(string username, string password, string roomId, bool isSecret = false, bool isAutoReady = true)
        {
            kanaLink = "https://kana.byha.top:444/";
            driver = new ChromeDriver();
            this.username = username;
            this.password = password;
            this.roomId = roomId;
            this.isSecret = isSecret;
            this.isAutoReady = isAutoReady;
            mpType = new int[25, 25];
            mpTmp = new int[25, 25];
            mpBelong = new int[25, 25];
            vis = new bool[25, 25];
            tmpVis = new bool[25, 25];
            error = false;
            sx = sy = 0;
            size = 20;
            endTag = false;
            ansLen = 100000;
        }

        private void SendKeyToTable(string key)
        {
            var ac = new Actions(driver);
            ac.SendKeys(key).Perform();
        }

        // https://blog.csdn.net/weixin_42107267/article/details/93198343
        // 生草方法
        public bool isElementExist(string elementID)
        {
            try
            {
                driver.FindElementById(elementID);
                return true;
            }
            catch
            {
                Console.WriteLine("找不到ID为{0}的组件", elementID);
                return false;
            }
        }

        // 字面意思
        private void getMap()
        {
            mpType = new int[25, 25];
            mpTmp = new int[25, 25];
            mpBelong = new int[25, 25];
            var s = driver.FindElementById("m").GetAttribute("innerHTML");
            var stype = new List<string>();
            var stmp = new List<string>();
            var cnt = 0;
            string g;
            while (true)
            {
                Match tmp = Regex.Match(s, @"class=""[\s\S]*?""");
                if (tmp.Success)
                {
                    g = tmp.Value;
                    g = g.Substring(7, g.Length - 1 - 7);
                    stype.Add(" " + g + " ");
                    int p = s.IndexOf(g);
                    s = s.Substring(p + g.Length, s.Length - (p + g.Length));
                    cnt += 1;
                }
                else
                {
                    break;
                }
                tmp = Regex.Match(s, ">.*?<");
                g = tmp.Value;
                g = g.Substring(1, g.Length - 1 - 1);
                stmp.Add(g);
            }
            size = (int)(Math.Pow(cnt, 0.5));
            if (!(size == 9 || size == 10 || size == 19 || size == 20))
            {
                return;
            }
            for (var i = 0; i < size; i++)
            {
                for (var j = 0; j < size; j++)
                {
                    string p = stype[0];
                    stype.RemoveAt(0);

                    if (p.Contains(" city ") | p.Contains(" empty-city "))
                    {
                        mpType[i + 1, j + 1] = 5;
                    }
                    else if (p.Contains(" crown "))
                    {
                        mpType[i + 1, j + 1] = 2;
                    }
                    else if (p.Contains(" mountain ") || p.Contains(" obstacle "))
                    {
                        mpType[i + 1, j + 1] = 1;
                    }
                    else if (p.Contains(" gas "))
                    {
                        mpType[i + 1, j + 1] = 1;
                    }
                    else if (p.Contains(" null ") && p.Contains(" grey "))
                    {
                        mpType[i + 1, j + 1] = 0;
                    }
                    else if (p.Contains(" null ") && !p.Contains(" grey "))
                    {
                        mpType[i + 1, j + 1] = 3;
                    }
                    else
                    {
                        mpType[i + 1, j + 1] = -1;
                    }
                    if (p.Contains(" own "))
                    {
                        mpBelong[i + 1, j + 1] = 1;
                    }
                    else
                    {
                        mpBelong[i + 1, j + 1] = 2;
                    }
                    p = stmp[0];
                    stmp.RemoveAt(0);
                    try
                    {
                        mpTmp[i + 1, j + 1] = int.Parse(p);
                    }
                    catch
                    {
                        //Console.WriteLine("format exception:{0}", e.Message);
                        mpTmp[i + 1, j + 1] = 0;
                    }
                }
            }
            return;
        }


        private void dfsRoute(int x, int y, int ex, int ey, int cnt)
        {
            if (x == ex && y == ey && cnt < ansLen)
            {
                ansLen = cnt;
                route = new List<int[]>();
                tmpQ.ForEach(i => route.Add(i));
                return;
            }
            if (cnt >= ansLen)
            {
                return;
            }
            var tmpI = new int[4] { 0, 1, 2, 3 };
            tmpI = tmpI.OrderBy(c => Guid.NewGuid()).ToArray<int>();

            foreach (var i in tmpI)
            {
                if (endTag)
                {
                    return;
                }
                var px = x + di[i, 0];
                var py = y + di[i, 1];
                if (px >= 1 && px <= size && py >= 1 && py <= size && (!tmpVis[px, py]) && mpType[px, py] != 1)
                {

                    tmpVis[px, py] = true;

                    tmpQ.Add(new int[3] { i, x, y });
                    dfsRoute(px, py, ex, ey, cnt + 1);
                    tmpQ.Remove(new int[3] { i, x, y });
                }
            }
        }

        private void Attack(int x, int y, int ex, int ey)
        {
            tmpQ = new List<int[]>();
            route = new List<int[]>();
            endTag = false;
            tmpVis = new bool[25, 25];
            tmpVis[x, y] = true;
            ansLen = 10000;
            dfsRoute(x, y, ex, ey, 0);
            if (route.Count < 1)
            {
                return;
            }
            foreach (var p in route)
            {
                var i = p[0];
                getMap();
                if (x < 1 || y < 1 || x > size || y > size || mpBelong[x, y] == 2 || mpTmp[x, y] < 2)
                {
                    return;
                }
                if (i == 0)
                {
                    Pr("W");
                    x -= 1;
                }
                else if (i == 1)
                {
                    Pr("D");
                    y += 1;
                }
                else if (i == 2)
                {
                    Pr("S");
                    x += 1;
                }
                else
                {
                    Pr("A");
                    y -= 1;
                    Thread.Sleep(TimeSpan.FromSeconds(0.25));
                }
            }
        }

        // 选择土地
        public void selectLand(int x, int y)
        {
            try
            {
                driver.FindElementById(($"td-{((x - 1) * size) + y}")).Click();
                return;
            }
            catch
            {
                Console.WriteLine("选择土地失败");
                return;
            }
        }

        //登录，如果出现异常则在5S后退出
        public void Login()
        {

            Console.WriteLine("正在登录…");
            driver.Url = kanaLink;
            var usernameBox = driver.FindElementByName("username");
            var passwordBox = driver.FindElementByName("pwd");
            var ac = new Actions(driver);

            // 输入账号密码并登录
            ac.SendKeys(usernameBox, username);
            ac.SendKeys(passwordBox, password);
            // 等待用户手动输入验证码
            Thread.Sleep(10000);
            ac.Click(driver.FindElementById("submitButton")).Perform();

            /*
            try:
                WebDriverWait(self.driver, 8).until(EC.url_to_be(self.kanaLink))
                print("登录成功！")
            except TimeoutException:
                print("网络连接出现问题或账密错误！\n程序将在5秒后退出")
                sleep(5)
                self.driver.close()
                del self
             */
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(8));
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.UrlToBe(kanaLink));
                Console.WriteLine("登录成功！");
            }
            catch
            {
                Console.WriteLine("网络连接出现问题或账密错误！");
                Thread.Sleep(5000);
                driver.Close();
                //del self 没有del 告辞
            }
        }

        // 进入指定房间
        public void EnterRoom()
        {
            driver.Url = "https://kana.byha.top:444/checkmate/room/" + roomId;

            if (isSecret)
            {
                var settingBtn = driver.FindElementByClassName("form-check-input");
                var ac = new Actions(driver);
                ac.Click(settingBtn).Perform();
            }
            Console.WriteLine("Bot已就位！");
        }

        // 准备开始，如果300秒未开始，程序退出
        public void Ready()
        {
            try
            {
                userCount = int.Parse(driver.FindElementById("total-user").Text);
            }
            catch
            {
                Console.WriteLine("获取玩家数失败");
                userCount = 3;
            }
            var ac = new Actions(driver);
            ac.Click(driver.FindElementById("ready")).Perform();

            try
            {
                /*
                WebDriverWait(self.driver, 300).until(
                    EC.visibility_of_element_located((By.TAG_NAME, "tbody")))
                */
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(300));
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(driver.FindElementsByTagName("tbody")));
            }
            catch (Exception e)
            {
                Console.WriteLine("房间内无人开始，过一会再试试吧:{0}", e.Message);
                Thread.Sleep(5000);
                Kill();
            }
        }

        private void Kill()
        {
            driver.Close();
            //del self 我tm del你妈
        }

        private void Pr(string c)
        {
            SendKeyToTable(c);
            // print(c)
        }

        private bool isOutside(int x, int y)
        {
            for (int i = 0; i < 4; i++)
            {

                int px = x + di[i, 0];
                int py = y + di[i, 1];
                if (px >= 1 && px <= size && py >= 1 && py <= size && mpBelong[px, py] == 2)
                {
                    return true;
                }
            }
            return false;
        }

        private void changeTarget()
        {
            var insideAnsTmp = mpTmp[sx, sy];
            var insideAnsX = sx;
            var insideAnsY = sy;
            var outsideAnsTmp = 0;
            var outsideAnsX = 0;
            var outsideAnsY = 0;
            for (int p = 0; p < size; p++)
            {
                for (int q = 0; q < size; q++)
                {
                    var i = p + 1;
                    var j = q + 1;
                    if (mpBelong[i, j] == 1)
                    {
                        if (isOutside(i, j))
                        {
                            if (mpTmp[i, j] > outsideAnsTmp)
                            {
                                outsideAnsTmp = mpTmp[i, j];
                                outsideAnsX = i;
                                outsideAnsY = j;
                            }
                        }
                        else
                        {
                            if (mpTmp[i, j] > insideAnsTmp)
                            {
                                insideAnsTmp = mpTmp[i, j];
                                insideAnsX = i;
                                insideAnsY = j;
                            }
                        }
                    }
                }
            }
            if (outsideAnsTmp * 5 >= insideAnsTmp)
            {
                sx = outsideAnsX;
                sy = outsideAnsY;
            }
            else
            {
                sx = insideAnsX;
                sy = insideAnsY;
            }
            q.Add(new int[2] { sx, sy });
            if (rd.Next(0, 2) == 1)
            {
                vis = new bool[25, 25];
            }
            vis[sx, sy] = true;
            selectLand(sx, sy);
            return;
        }

        private void botMove()
        {
            Thread.Sleep(220);
            var x = 0;
            var y = 0;
            var tryTime = 0;
            getMap();
            while (true)
            {
                //Thread.Sleep(1);
                if (q.Count == 0)
                {
                    changeTarget();
                }
                x = q[0][0];
                y = q[0][1];
                tryTime += 1;
                q.RemoveAt(0);
                if (!(mpTmp[x, y] <= 1 && mpType[x, y] != 2 && tryTime <= 10))
                {
                    break;
                }
            }
            if (tryTime > 10)
            {
                return;
            }
            if (mpTmp[x, y] <= 1)
            {
                return;
            }
            if (mpBelong[x, y] == 2)
            {
                return;
            }
            homes = new List<int[]>();
            if (mpType[x, y] == 2 && mpBelong[x, y] == 1)
            {
                Pr("Z");
            }
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (mpType[i + 1, j + 1] == 2 && mpBelong[i + 1, j + 1] == 2)
                    {
                        homes.Add(new int[2] { i + 1, j + 1 });
                    }
                }
            }
            if (homes.Count > 0 && rd.Next(1, 11) == 1 && mpTmp[x, y] > 30)
            {
                var g = rd.Next(0, homes.Count);
                Attack(x, y, homes[g][0], homes[g][1]);
                return;
            }
            var ansTmp = 0;
            var ansI = -1;
            int[] tmpI = { 0, 1, 2, 3 };
            // random.shuffle(tmpI)
            tmpI = tmpI.OrderBy(c => Guid.NewGuid()).ToArray<int>();

            int px, py;
            foreach (var i in tmpI)
            {
                px = x + di[i, 0];
                py = y + di[i, 1];
                if (px >= 1 && px <= size && py >= 1 && py <= size && mpType[px, py] != 1 && (!vis[px, py]) && (mpType[px, py] != 5 || mpTmp[x, y] > mpTmp[px, py]))
                {
                    var currentTmp = 0;
                    if (mpBelong[px, py] == 2)
                    {
                        if (mpType[px, py] == 2)
                        {
                            currentTmp = 10;
                            if (!(homes.Contains(new int[2] { px, py })))
                            {
                                homes.Add(new int[2] { px, py });
                            }
                        }
                        else if (mpType[px, py] == 5)
                        {
                            currentTmp = 8;
                        }
                        else if (mpType[px, py] == 3)
                        {
                            currentTmp = 5;
                        }
                        else
                        {
                            currentTmp = 3;
                        }
                    }
                    else
                    {
                        currentTmp = 1;
                    }
                    if (currentTmp > ansTmp)
                    {
                        ansTmp = currentTmp;
                        ansI = i;
                    }
                }
            }
            if (ansI == -1)
            {
                return;
            }
            px = x + di[ansI, 0];
            py = y + di[ansI, 1];
            vis[px, py] = true;
            q.Add(new int[] { px, py });
            if (ansI == 0)
            {
                Pr("W");
            }
            else if (ansI == 1)
            {
                Pr("D");
            }
            else if (ansI == 2)
            {
                Pr("S");
            }
            else
            {
                Pr("A");
            }
            botMove();
            return;
        }

        public void Init()
        {

            Login();
            EnterRoom();
            table = driver.FindElementByTagName("tbody");
            while (true)
            {
                if (isAutoReady)
                {
                    Ready();
                }
                Pr("F");// 防踢
                getMap();
                sx = 0;
                sy = 0;
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        if (mpBelong[i + 1, j + 1] == 1 && mpType[i + 1, j + 1] == 2)
                        {
                            sx = i + 1;
                            sy = j + 1;
                        }
                    }
                }
                if (sx == 0 || sy == 0)
                {
                    continue;
                }
                changeTarget();
                botMove();
            }
        }
    }
}
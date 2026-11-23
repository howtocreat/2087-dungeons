import time,random
print("2087地下城1.3.4(正式版)")
nm={
'message1':'输入错误',
'message2':'已自动返回上一对话框^_^\n',
}
capsule=0#食物胶囊
aklv=0
ak=7+2*aklv
robotlv=0
#robotlv=4
manlv=0#定义等级
gp=0.01#出金概率
dp=0#出钻概率
dig=0#机器人挖矿概率
rs=0#机器人速度
silver=0
gold=0
diamond=0#货币初始化
enemy=0#敌人血量
hjlv=0
aenemy=6-hjlv#敌人攻击
#silver=150
#gold=100
#diamond=5
fuc=0
fuck=0#单次执行符
main=0#主程序循环
cave=0#矿洞程序循环
shop=0#商店程序循环
back=0#背包循环
r=0#机器人循环
zhan=0#战区循环
robot=0#定义机器人
robott=0#命令机器人
talk=0#定义对话框
a=0#基础防误触变量
c=0
s=0
t=0
b=0
cs=0
i=0
d=0#其他变量
food=100
blood=20#设置生命值
bloodbottle=0
ddiamond=0
ggold=0
ssilver=0#临时矿物数
z=0#战区新手教程
name='fafa(系统默认名)'
#a=10;main=1
#初始设置
def backage():
    global silver,gold,diamond,capsule,back,npc,blood,food,bloodbottle
    back=1
    print("-------------背包--------------\n")
    print("(系统)您的背包中有"+str(silver)+"块银;"+str(gold)+"块金;"+str(diamond)+"颗钻石")
    print("食物胶囊有"+str(capsule)+"个(每颗回复10点饱食度;食用输入s)\n")
    print("血瓶有"+str(bloodbottle)+"个(每颗回复10点血;食用输入x)\n")
    while back==1:
        print("-------------背包--------------\n")
        npc=input("(输入框)")
        if npc=="f":
            print("(系统)你现在有"+str(blood)+"点血;"+str(food)+"点饱食度\n")
        if npc=="s":
            c=0
            if capsule>=1:                  
                c=1         
                capsule=capsule-1
                if 90<=food<=100:
                    food=100
                    print("(系统)已成功食用一个食物胶囊\n")
                    c=0
                if food<90:
                    food=food+10
                    print("(系统)已成功食用一个食物胶囊\n")
                    c=0
            elif capsule<1 and c==0:
                print("(系统)食物胶囊已不足\n") 
                back=1
        if npc=="x":
            c=0
            if bloodbottle>=1:                  
                c=1         
                bloodbottle=bloodbottle-1
                if 10<=blood<=20:
                    blood=20
                    print("(系统)已成功食用一个血瓶\n")
                    c=0
                if blood<10:
                    blood=blood+10
                    print("(系统)已成功食用一个血瓶\n")
                    c=0
            elif bloodbottle<1 and c==0:
                print("(系统)血瓶已不足\n") 
                back=1            
        if npc=="e":          
            back=0         
            npc=0   
#定义背包函数
def zhuangtai():
    global silver,gold,diamond,capsule,back,talk,blood,food
    print("(系统)你现在有"+str(blood)+"点血;"+str(food)+"点饱食度\n")
#定义状态函数
def xueliang():
    global silver,gold,diamond,capsule,back,talk,blood,food,fuck,fuc,cave,b
    if food>80:
        fuck=1
    if food>20:
        fuc=1
    if food<=20 and fuc==1:
        print("(系统)请尽快补充食物\n")
        fuc=0
    elif food<=80 and fuck==1:
        print("(系统)你肚子有点饿了\n")
        fuck=0
    if food<=0:
        blood=0
    if blood<=0:
        npc=input("(系统)你已死亡,将被逐出这个世界\n")        
        str(bloodbotlle)
        zhan=zhi
#定义血量函数
def robottt():
    global silver,gold,diamond,times,rs,gp,robott,dig,cave,r,food,robotlv,manlv,dp,ddiamond,ggold,ssilver
    rs=1-robotlv*0.1
    gp=0.1+robotlv*0.01+manlv*0.02
    dp=0.01+robotlv*0.001+manlv*0.002
    ddiamond=0
    ggold=0
    ssilver=0    
    food+=0.5
    while robott=="start":
        time.sleep(rs)
        dig=random.randint(1,10000)
        times=times-1
        food=food-0.5
        xueliang()
        if times==0:
            print("(系统)挖矿结束")
            robott=0
            r=0
            q=1
            print(name+'为你挖了'+str(ddiamond)+'颗钻石;'+str(ggold)+'块金'+str(ssilver)+'块银')
        if 0<=dig<=dp*10000:
            diamond+=1      
            ddiamond+=1  
        elif 0<=dig<=gp*10000:
            gold+=1
            ggold+=1
        else :
            silver+=1
            ssilver+=1
#机器人模块
pk=1
while pk==1:
    print('(系统)输入j跳过新手教程,c进入游戏存档,n开启新游戏')
    npc=input('(输入框)')
    if npc=='j':
        a=-1
        main=1
        pk=0
    elif npc=="c":
        print('(系统)请输入你的存档')
        npc=input('(输入框)')    
        fan=[]
        for i in range(1,10):
            fan.append(str(i))
        if npc in fan:
            bloodbottle=0
            capsule=0#食物胶囊
            aklv=0
            hjlv=0            
            robotlv=0
            manlv=0#等级
            silver=0
            gold=0
            diamond=0#货币
            aenemy=6-hjlv#敌人攻击
            name='fafa(系统默认名)'
            a=-1
            main=1
            pk=0          
            print('(系统)以进入你的存档^0^')
        else:
            print('(系统)输入错误')
    elif npc=="n":
        print('(系统)进入新手教程\n')
        npc=input("(陌生人,轻声)你终于醒了啊")
        npc=input("(陌生人)我叫小帅,是这里的接待员")
        player=input("(你眨了眨眼,你身处一个简陋的医务室)我这是……?")
        npc=input("(小帅)地上的感染者突变了,我们只能住在这个简陋的地下城了")
        npc=input("(小帅)巡逻部队清理战区时发现了你,真是神奇,你竟然没被感染")
        player=input('(你)真希望能返回战区去同感染者战斗！')
        npc=input('(小帅)你现在还不熟悉这儿的环境')
        npc=input("(小帅)但我们先给你配了一把AK-47,到时候在战区中用他击败感染者")
        npc=input("(小帅,笑,无奈)害,只有这么远古的枪了,或许升级下战斗力还不错")
        npc=input("(小帅)想办法先在这里生存下来吧,我们会让这个地方变好的")
        npc=input("(小帅)我会指导你学会基本的生存方式")
        pk=0
    else:
        print('(系统)输入错误\n') 
while a==0:
    print("-------------主城区--------------\n")
    a=1
    player=input("(你看了看主城区,设施还挺全)哎,旁边那是什么地方?")
    npc=input("(小帅)这是地下城的矿区")
    npc=input("(小帅)在这里,挖矿是最基础的经济来源")
    print("(小帅)输入k进入矿区")
    talk=input("(输入框)")
    if talk=="k":
        print("-------------矿区--------------")
        player=input("(你注意到有些机器在自动工作)这些是…?")
        npc=input("(小帅)这都是私有的机器人,能帮助你挖矿,记得去集市看看!")
        npc=input("(小帅)前期手动挖矿是最快的")
        while a==1:
            a=2
            print("(小帅)试试输入g开始手动挖矿")
            talk=input("(输入框)")
            if talk=="g":
                silver+=1
                print('银+1')
                npc=input("(小帅)你挖到了第一块银,银作为这个世界最基本的货币非常重要!")
                npc=input("(小帅)你做很多事都会消耗饱食度,记得补充食物!(任何时候输入f查看状态!)")
                while a==2:
                    a=3
                    print("(小帅)任何时候你可以输入b以查看背包,试试看")
                    talk=input("(输入框)")
                    if talk=="b":
                        print("-------------背包--------------\n")
                        print("(系统)您的背包中有"+str(silver)+"块银;"+str(gold)+"块金;"+str(diamond)+"颗钻石")
                        print("食物胶囊有"+str(capsule)+"个(每颗回复10点饱食度;食用输入s)\n")    
                        while a==3:
                            a=4
                            print("(小帅)任何时候输入e返回上一界面!")
                            talk=input("(输入框)")
                            if talk=="e":
                                print("-------------矿区--------------")
                                npc=input("(小帅)你已经回到了矿区")
                                npc=input("(小帅)自动转回主城区中")
                                npc=input("(小帅)你现在可以试着去挖矿,或是到处去转转啦,拜拜")
                                main=1
                            else :
                                print(nm['message1'])
                                print(nm['message2'])
                                a=3
                    else :
                        print(nm['message1'])
                        print(nm['message2'])
                        a=2
            else :
                print(nm['message1'])
                print(nm['message2'])
                a=1
    else :
        print("嘿，别乱点了,小心被卡出这个世界！")
        print(nm['message2'])
        a=0
#基础教程
while main==1:
    xueliang()
    if d==1 or b==1 :
        break
    print("-------------主城区--------------\n")
    talk=input("(系统)输入k进入矿区;b查看背包;s进入集市;z进入战区\n")
    if talk=="hbzs":
        print("开启彩蛋")
        robot=1
        robotlv=0
        manlv=0
        silver=99999
        gold=99999
        diamond=99999    
        food=10
        name="测试"
    if talk=="b":
        backage()
    if talk=="k":
        cave=1
        while cave==1:
            xueliang()
            if b==1:
                cave=0
                d=1
            print("-------------矿区--------------\n")
            rs=1-robotlv*0.1
            gp=0.1+robotlv*0.01+manlv*0.02
            dp=0.01+robotlv*0.001+manlv*0.002
            print("(矿工队长)输入g以挖矿;输入r以开启机器人挖矿")
            talk=input("(输入框)")
            if talk=="r":
                if robot>=1:
                    r=1
                    while r==1:                      
                        times=input("(机器人界面)请选择挖矿次数"+"1~"+str(10*(robotlv+1))+"(升级机器人挖矿等级以提升次数):")  
                        if times=="e":
                            r=0
                            break
                        fang=[]
                        for i in range(1,10*(robotlv+1)+1):
                            fang.append(str(i))
                        if times in fang:
                            times=int(times)+1
                            q=1
                            if food>times*0.5 and q==1:
                                q=0
                                print("(系统)"+name+"已开始工作")
                                robott="start"
                                robottt()            
                            elif food<=times*0.5 :
                                print('(系统)体力值不能支持'+str(times-1)+'次挖矿,请重试')
                        else :
                            print("(系统)次数错误\n")                                                                                   
                else :
                    print("(系统)请先购买机器人")
            if talk=="f":
                zhuangtai()
            if talk=="g":
                dig=random.randint(1,10000)
                if 0<=dig<=dp*10000:
                    diamond+=1
                    print('(背包)钻石+1 !!!^_^')        
                elif 0<=dig<=gp*10000:
                    gold+=1
                    print('(背包)金+1 !')
                else :
                    silver+=1 
                    print('(背包)银+1')
                food=food-1
            if talk=="b":
                backage()
            if talk=="e":
                cave=0
    if talk=="s":
        shop=1
        print("(集市老板)欢迎来到集市!这里有全自动的机器人,食物胶囊,升级装置等，欢迎选购！\n")
        while shop==1:
            print("-------------集市--------------\n")
            print("(集市老板)输入r进入机器人系统;输入h进入银行;输入a进入武器店;输入s进入食品店\n")
            talk=input("(输入框)")
            if talk=="b":              
                backage()
            if talk=="e":
                shop=0       
            if talk=="s":
                foodshop=1
                while foodshop==1:
                    print("-------------食品店--------------\n")
                    print('(食品店老板)输入j购买食物胶囊(5银/个);输入x购买血瓶(2钻/个)，在背包中使用!')
                    npc=input('(输入框)')
                    if npc=="b":
                        backage()
                    if npc=="f":
                        zhuangtai()
                    if npc=="e":
                        foodshop=0
                    if npc=="j":         
                        if silver>=5:
                            silver=silver-5
                            capsule+=1
                            print("(集市老板)成功购买了一个\n")
                        else :
                            print("(集市老板)货币已不足")
                    if npc=="x":
                        if diamond>=2:
                            diamond=diamond-2
                            bloodbottle+=1
                            print("(集市老板)成功购买了一个\n")
                        else :
                            print("(集市老板)货币已不足")            
            if talk=="a":
                armyshop=1
                while armyshop==1:
                    print('-------------武器店--------------\n')
                    print('(武器店职员)输入r查看升级;输入h升级护甲('+str(hjlv+1)+'颗钻石/级);输入a升级枪械('+str(aklv+1)+'颗钻石/级)\n')       
                    npc=input('(输入框)')
                    if npc=="r":
                        print('(系统)你现在的护甲等级为lv.'+str(hjlv+1)+'/lv.5;'+'枪械等级为lv.'+str(aklv+1)+'/lv.10\n')
                        print('(系统)你的防御力为'+str(hjlv)+'点;攻击力为'+str(ak)+'点')
                    if npc=="h":
                        if diamond>=(hjlv+1) and hjlv<4:
                            diamond=diamond-(hjlv+1)
                            hjlv+=1
                            aenemy=6-hjlv
                            print('(武器店职员)成功升级护甲等级\n')
                        elif diamond<(hjlv+1):
                            print('(武器店职员)钱带够了再来哦')
                        elif hjlv>=4: 
                            print('(武器店职员)已升至满级啦')
                    if npc=="a":
                        if diamond>=(aklv+1) and aklv<9:
                            diamond=diamond-(aklv+1)
                            aklv+=1
                            ak=7+aklv*2
                            print('(武器店职员)成功升级武器等级\n')
                        elif diamond<(aklv+1):
                            print('(武器店职员)钱带够了再来哦')
                        elif aklv>=4: 
                            print('(武器店职员)已升至满级啦')            
                    if npc=="e":
                        armyshop=0
                        shop=1
                    if npc=="b":
                        backage()
                    if npc=="f":
                        zhuangtai()
            if talk=="h":
                bank=1
                while bank==1:
                    print('-------------银行--------------\n')
                    print("(银行行长)输入g将10块银换成1个金;输入d将10个金换成一颗钻石;输入s将100个银换成一个钻石;输入gb或db或sb反向交换")
                    npc=input("(输入框)")
                    if npc=="s":
                        if silver>=100 :
                            t=1
                            silver=silver-100
                            diamond+=1
                            food=food-1
                            xueliang()
                            print("(银行行长)兑换成功")
                            shop=1
                            t=0
                        elif silver<100 and t==0:
                            print("(银行行长)货币已不足")
                    if npc=="sb":
                        if diamond>=1 :
                            s=1
                            diamond=diamond-1
                            silver+=100
                            food=food-1
                            xueliang()
                            print("(银行行长)兑换成功")
                            shop=1
                            s=0            
                    if npc=="g":
                        if silver>=10 :
                            s=1
                            silver=silver-10
                            gold+=1
                            food=food-1
                            xueliang()
                            print("(银行行长)兑换成功")
                            shop=1
                            s=0
                        elif silver<10 and s==0:
                            print("(银行行长)货币已不足")
                    if npc=="d":
                        if gold>=10 :
                            t=1
                            gold=gold-10
                            diamond+=1
                            food=food-1
                            xueliang()
                            print("(银行行长)兑换成功")
                            shop=1
                            t=0
                        elif gold<10 and t==0:
                            print("(银行行长)货币已不足")
                    if npc=="gb":
                        if gold>=1 :
                            s=1
                            gold=gold-1
                            silver+=10
                            food=food-1
                            xueliang()
                            print("(银行行长)兑换成功")
                            shop=1
                            s=0
                        elif gold<1 and s==0:
                            print("(银行行长)货币已不足")
                    if npc=="db":
                        if diamond>=1 :
                            t=1
                            diamond=diamond-1
                            gold+=10
                            food=food-1
                            xueliang()
                            print("(银行行长)兑换成功")
                            shop=1
                            t=0
                        elif diamond<1 and t==0:    
                            print("(银行行长)货币已不足")            
                    if npc=="e":
                        bank=0
                        shop=1
                    if npc=="b":
                        backage()
                    if npc=="f":
                        zhuangtai()
            if talk=="f":
                zhuangtai()
            if talk=="r":            
                print('-------------机器人系统--------------\n')
                print("(机器人系统)输入t购买机器人,输入f查看升级")
                print("(机器人系统)基础机器人1秒钟可以挖一块银,花费100个银购买你的机器人!")
                talk=input("(输入框)")
                if talk=="t":
                    if silver>=100 and robot==0:
                        silver=silver-100
                        robot=1
                        print("(机器人系统)你已经成功购买了机器人")
                        print("(机器人系统)给你的机器人起个名字吧")
                        name=input('(输入框)')
                        print('你的机器人名字叫'+name)
                        print("(机器人系统)Ta在你进入矿区后就会开启")
                    elif silver<100 and robot==0:
                        print("(机器人系统)你的银子还不够,攒够了再来吧！")
                    elif robot==1:
                        print("(机器人系统)你已经不能再拥有更多机器人了")
                if talk=="f":
                    rs=1-robotlv*0.1
                    gp=0.1+robotlv*0.01+manlv*0.02
                    dp=0.01+robotlv*0.001+manlv*0.002
                    print("(机器人系统)你的基础挖矿等级为lv."+str(manlv+1)+"/lv.10;你的机器人挖矿等级为lv."+str(robotlv+1)+"/lv.8")    
                    print("(机器人系统)挖矿出金概率为"+str(round(gp,2))+";出钻概率为"+str(round(dp,4)))
                    print("(机器人系统)机器人挖矿速度为"+str(round(rs,1))+"s/块")
                    print("(机器人系统)输入m升级基础挖矿等级(消耗"+str(manlv+2)+"块金);输入r升级机器人挖矿等级(消耗"+str(robotlv+4)+"块金)")
                    talk=input("(输入框)")
                    if talk=="m":
                        if manlv<9:
                            if gold>=(manlv+2):
                                gold=gold-(manlv+2)
                                manlv+=1
                                print("(机器人系统)成功升级基础挖矿\n")
                            elif gold<(manlv+2):
                                print("(机器人系统)货币已不足\n")
                        else :
                            print("(机器人系统)已升至满级\n")
                    if talk=="r":
                        if robotlv<7:
                            if gold>=(robotlv+4):
                                gold=gold-(robotlv+4)
                                robotlv+=1
                                print("(机器人系统)成功升级机器人挖矿\n")                   
                            elif gold<(robotlv+4):
                                print("(机器人系统)货币已不足\n")     
                        else :
                            print("(机器人系统)已升至满级\n")                   
    if talk=="f":
        print("(系统)你现在有"+str(blood)+"点血;"+str(food)+"点饱食度\n")
    if talk=="z":
        if robotlv<3:
            print('(系统)将机器人等级升至4级就可以重返战区!')
        if z==0 and robotlv>=3:
            npc=input("(小帅)Hi~,又见面了,不过这次可要小心点了,到处可能都有危险！")
            npc=input("(小帅)饱食度在80及以上时才能进入战区!")
            if food<80:
                print("(巡逻部队)请先补充你的饱食度\n")
            else :
                npc=input("(小帅)按照系统的提示操作,点错会收到伤害!")
                npc=input("(小帅)输入s以闪避,k以攻击")
                npc=input('(小帅)若失去了血量，在背包中使用血瓶补充!')
                npc=input('(小帅)为了专心战斗，你会在状态变化时直接得知,并且不能打开背包!')
                npc=input("(小帅)开始游戏吧\n")
                z=1
                zhan=1
        if z==1 and robotlv>=3:
            zhan=1
            if food<80:
                print("(巡逻部队)请先补充你的饱食度\n")      
            else :
                enemy=random.randint(50,100)
                while zhan==1:
                    if enemy<=0:
                        ddiamond=random.randint(1,1+int((1+aklv)/2))
                        diamond=diamond+ddiamond
                        print("(系统)成功打败感染者")
                        print("(系统)你获得了"+str(ddiamond)+"颗钻石作为奖励")
                        zhan=0
                    else :
                        cs=random.randint(1,100)
                        print('(显示器)感染者的血量为'+str(enemy))
                        if 1<=cs<=33:
                            print("(系统)感染者向你冲过来了,输入s闪避!")
                            i=input('(输入框)')
                            if i=="s":
                                print("(系统)成功闪避")
                                food=food-1
                                zhuangtai()
                                xueliang()
                            else :
                                print("(系统)你受到了"+str(aenemy)+"点伤害!")
                                blood=blood-aenemy
                                food=food-aenemy
                                zhuangtai()
                                xueliang()
                        if 33<cs<=100:
                            print("(系统)感染者漏出了破绽！,输入k攻击!")
                            i=input('(输入框)')
                            if i=="k":
                                print("(系统)成功攻击")
                                print("(系统)感染者受到了"+str(ak)+"点伤害")
                                enemy=enemy-ak
                                food=food-ak/4
                                zhuangtai()
                                xueliang()
                            else :
                                print("(系统)未及时攻击,错失机会!")
                                food=food-ak/2
                                zhuangtai()
                                xueliang()
            
            
            
            
                                                     

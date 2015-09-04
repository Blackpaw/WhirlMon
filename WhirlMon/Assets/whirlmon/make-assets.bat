@echo off

Rem Square 71
call make_sq_asset.bat 71 400 284
call make_sq_asset.bat 71 200 142
call make_sq_asset.bat 71 100 71
call make_sq_asset.bat 71 150 107
call make_sq_asset.bat 71 125 89

Rem Square 150
call make_sq_asset.bat 150 400 600
call make_sq_asset.bat 150 200 300
call make_sq_asset.bat 150 100 150
call make_sq_asset.bat 150 150 225
call make_sq_asset.bat 150 125 188

Rem Wide 310x150
call make_wide_asset.bat 310x150 400 1240 600
call make_wide_asset.bat 310x150 200 620 300
call make_wide_asset.bat 310x150 100 310 150
call make_wide_asset.bat 310x150 150 465 225
call make_wide_asset.bat 310x150 125 388 188

Rem Square 310
call make_sq_asset.bat 310 400 1240
call make_sq_asset.bat 310 200 620
call make_sq_asset.bat 310 100 310
call make_sq_asset.bat 310 150 465
call make_sq_asset.bat 310 125 388

Rem Square 44
call make_sq_asset.bat 44 400 176
call make_sq_asset.bat 44 200 88
call make_sq_asset.bat 44 100 44
call make_sq_asset.bat 44 150 66
call make_sq_asset.bat 44 125 55
rem Target 44
call make_sq_target_asset.bat 44 256
call make_sq_target_asset.bat 44 48
call make_sq_target_asset.bat 44 24
call make_sq_target_asset.bat 44 16

rem Store Logo
call make_sq_asset2.bat StoreLogo 400 200
call make_sq_asset2.bat StoreLogo 200 100
call make_sq_asset2.bat StoreLogo 100 50
call make_sq_asset2.bat StoreLogo 150 75
call make_sq_asset2.bat StoreLogo 125 63


rem Badge Logo
call make_sq_asset2.bat BadgeLogo 400 96
call make_sq_asset2.bat BadgeLogo 200 48
call make_sq_asset2.bat BadgeLogo 100 24
call make_sq_asset2.bat BadgeLogo 150 36
call make_sq_asset2.bat BadgeLogo 125 30

Rem Splash
call make_wide_asset2.bat SplashScreen 400 2480 1200
call make_wide_asset2.bat SplashScreen 200 1240 600
call make_wide_asset2.bat SplashScreen 150 930 450
call make_wide_asset2.bat SplashScreen 125 775 375
call make_wide_asset2.bat SplashScreen 100 620 300

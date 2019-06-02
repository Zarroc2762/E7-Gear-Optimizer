# E7-Gear-Optimizer

## Requirements

* .NET Framework 4.7.2 installed: http://go.microsoft.com/fwlink/?linkid=863265
* 64 Bit Operating System

 
## Features

* Pulls data like heroes, base stats etc. from Epic Seven DB, so it will be up to date when Epic Seven DB is updated
* Heroes/Items can be edited after you input them
* Can import JSON file from /u/HyrTheWinter's Optimzier, so you won't have to input all your gear again if you used it in the past Stat/Set filters will be evaluated while the optimization process is running. This means a calculated combination of items is discarded if it doesn't meet the requirements of the filter before the next one is calculated. It will use much less RAM this way. Unfortunately, I did not find a way to filter out these combinations before running the optimization. Time used will be the same as if no stat/set filter was set

 
## How to use

The application consists of four tabs. When you open it you will be shown the General tab.
### General

https://i.imgur.com/CURNynR.png

Here you will find a short explanation on how to use the program and you can import an existing JSON file. After you're done with optimizing, inputting gear etc. you should export your collection. The application will save your collection in it's directory when you close it but you should export because this is meant as a Backup and will always contain the latest state.
### Inventory

https://i.imgur.com/72Agkaw.png

In the Inventory tab, you will see all the items you imported. This is where you create/edit your gear. You can filter the shown items with the tabs at the top. When you select an item in the list the controls at the bottom will change to reflect the data of that item. You can edit an item by changing these controls and then clicking the edit (pen) button in the bottom right corner. Similarly it will create a new item when you click the + button.
### Heroes

https://i.imgur.com/7HfI9aJ.png

The Heroes tab allows you to see the stats of each of your heroes and their equipped items. Most of the controls at the bottom function similarly to the ones in the Inventory tab and will create/edit your heroes except the equip and edit buttons under each gear slot. The equip button will bring up a new window with a list of all your items which can be equipped in that slot. if you select one the hero will equip that item. Clicking the edit button will bring you to the Inventory tab and show you the selected item, so you can edit it.
### Optimization

https://i.imgur.com/ANV37tg.png

This is where everything comes together. Select your hero, configure filters and click optimize to receive a list of all possible equipment combinations which meet your criteria.

I recommend to keep the estimated results below 5,000,000 or to enter some stat filters because it will take a large amount of RAM if you don't. For reference: In the above screenshot, I calculated 7,3M combinations with no filters. It took ~7 min (depending on your CPU) and ~6GB RAM. When you enter a stat filter, it will still take ~7 min because it still has to calculate all combinations but it will take less RAM because combinations, which do not meet the requirement, are discarded directly after they're calculated.

When you select a result in the list, it will show the used items on the right side. Click the equip button on the bottom if you want to equip the selected result to your hero.

The results list can be sorted by clicking on the header of a column. There are two stats, which you won't find ingame: EHP and DMG.

EHP means Effective Health Points and is calculated with the following formula: HP * (1 + DEF/300). You can interpret this as a combination of the Health and Def stats, which will show you how "tanky" your character is.

DMG is calculated with the following Formula: ATK * (1 - CritChance) + ATK * CritChance * CritDamage. You can interpret this as the potential damage your character could do with a single hit. Ingame this is modified by Skill Multipliers.

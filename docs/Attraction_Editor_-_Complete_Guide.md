# Attraction Editor - Complete Guide

## 🎨 Overview

The Attraction Editor allows you to **completely customize** all attractions in the game:
- Edit names (English & German)
- Change pips (value)
- Adjust priority (1-3)
- Toggle €1 bonus
- Set gray effects
- Add new attractions
- Remove attractions
- Import/Export CSV

---

## 🚀 How to Access

**From Main Game:**
1. Click **"⚙️ Edit Attractions"** button in top bar
2. Editor window opens with all current attractions
3. Make changes
4. Click "Close" - changes apply to next new game

---

## 📋 Editor Interface

### **Left Panel: Category Filter**
- Filter by category (All/Nature/Water/Culture/Gastronomy/Gray)
- View statistics (count per category, bonus attractions)

### **Center Panel: Attraction List**
- DataGrid showing all attractions
- Sortable columns:
  - ID
  - Category
  - Name (English)
  - Name (German/Danish)
  - Pips (value)
  - Priority
  - €1 Bonus checkbox
  - Gray Effect

### **Right Panel: Edit Selected**
- Edit all properties of selected attraction
- Save Changes button
- Delete Attraction button
- Duplicate Attraction button

---

## ✏️ Editing Attractions

### **To Edit an Attraction:**

1. **Select** attraction in the center grid
2. **Edit fields** in right panel:
   - **English Name**: English translation
   - **German Name**: Your Danish/German name
   - **Pips**: How many pips tourists need (1-6)
   - **Priority**: Visit order (1=first, 2=second, 3=last)
   - **€1 Bonus**: Check if pays extra €1
   - **Category**: Nature/Water/Culture/Gastronomy/Gray
   - **Gray Effect**: Only for gray attractions
3. **Click "Save Changes"**

**Example:**
- Want Wanderung to pay €5 instead of €4?
- Change Pips from 3 to 4
- OR keep Pips at 3 and check €1 Bonus

---

## ➕ Adding New Attractions

### **Method 1: Menu**
1. Click **Add** menu
2. Select category:
   - Add Nature Attraction
   - Add Water Attraction
   - Add Culture Attraction
   - Add Gastronomy Attraction
   - Add Gray Attraction
3. New attraction appears with defaults
4. Edit it immediately

### **Method 2: Duplicate**
1. Select existing attraction
2. Click **"Duplicate Attraction"**
3. Duplicate appears with same properties
4. Edit the duplicate

**Use Case:**
- You have 3 "Park" attractions
- Want to add a 4th
- Select "Park", click Duplicate
- Done!

---

## 🗑️ Removing Attractions

1. Select attraction to remove
2. Click **"Delete Attraction"**
3. Confirm deletion
4. Attraction removed from all lists

---

## 💾 Import/Export CSV

### **Export to CSV:**
1. Click **File → Save to CSV**
2. Choose location
3. Opens CSV with format:
   ```
   Category,NameEnglish,NameGerman,Value,Priority,PaysBonusEuro,GrantedAction,Notes
   Nature,Hiking,Vandretur,3,1,True,None,
   Gastronomy,Brewery,Bryggerri,3,2,True,None,
   Gray,Casino,Casino,2,1,False,RerollTourist,
   ```

### **Import from CSV:**
1. Edit CSV in Excel/Notepad
2. Click **File → Load from CSV**
3. Select your CSV file
4. All attractions replaced with CSV data

**CSV Format:**
- **Column 1**: Category (Nature/Water/Culture/Gastronomy/Gray)
- **Column 2**: English name
- **Column 3**: German/Danish name
- **Column 4**: Pips (1-6)
- **Column 5**: Priority (1-3)
- **Column 6**: Bonus Euro (True/False)
- **Column 7**: Gray Effect (None or action name)
- **Column 8**: Notes (optional, ignored)

---

## 🎯 Common Editing Tasks

### **1. Change Attraction Values**

**Make Castles More Expensive:**
- Filter by Culture category
- Find all 3 "Slot" (Castle) attractions
- Change Pips from 5 to 6
- Save each one

### **2. Add More Bonus Attractions**

**Add bonus to all Cafés:**
- Filter by Gastronomy
- Find all 4 "Café" attractions
- Check "€1 Bonus" for each
- Save

### **3. Create New Gray Attraction**

**Add "Town Hall" that gives extra money:**
- Click Add → Add Gray Attraction
- Name: "Town Hall" / "Rådhus"
- Pips: 3
- Priority: 1
- Gray Effect: BuildAttraction (or whatever you want)
- Save

### **4. Remove Unwanted Attractions**

**Remove one Casino:**
- Filter by Gray
- Select one "Casino" (either 2-pip or 3-pip)
- Delete
- Now only 1 Casino in deck

### **5. Rebalance Categories**

**Make Water cheaper:**
- Filter by Water
- Change all 5-pip to 4-pip
- Change all 4-pip to 3-pip
- Test game balance

---

## 🔧 Gray Attraction Effects

Available effects for Gray attractions:

| Effect | Description |
|--------|-------------|
| **None** | No special effect (don't use for gray) |
| **BuildAttraction** | Normal build action |
| **RerollTourist** | Casino effect |
| **GiveTour** | Hotel effect |
| **AddTwoPipsGreen** | Nature Center |
| **AddTwoPipsBlue** | Water Center |
| **AddTwoPipsRed** | Culture Center |
| **AddTwoPipsYellow** | Food Center |
| **BusDispatch** | Move another bus |
| **BuildAttractionDiscount** | Contractor (€1 off) |

**To create new effect:**
- Currently limited to these 10 effects
- Need code changes to add new effects
- But can combine existing effects creatively

---

## 📊 Example CSV Files

### **Minimal Set (Fast Games)**
```csv
Category,NameEnglish,NameGerman,Value,Priority,PaysBonusEuro,GrantedAction,Notes
Nature,Park,Park,1,1,False,None,
Nature,Park,Park,1,1,False,None,
Nature,Park,Park,1,1,False,None,
Water,Beach,Strand,1,1,False,None,
Water,Beach,Strand,1,1,False,None,
Culture,City,By,3,1,False,None,
Gastronomy,Café,Café,2,1,False,None,
Gastronomy,Café,Café,2,1,False,None,
Gray,Casino,Casino,2,1,False,RerollTourist,
Gray,Hotel,Hotel,3,1,False,GiveTour,
```

### **High-Value Set (Epic Games)**
```csv
Category,NameEnglish,NameGerman,Value,Priority,PaysBonusEuro,GrantedAction,Notes
Nature,Mountain Trek,Bjergvandring,6,1,True,None,Extreme
Water,Yacht Trip,Yachttur,6,1,True,None,Luxury
Culture,Palace,Palads,6,1,False,None,Royal
Gastronomy,Chef's Table,Kokkebord,6,1,True,None,Premium
```

---

## 🧪 Testing Scenarios

### **Scenario 1: All Bonus Attractions**
1. Check €1 Bonus for ALL attractions
2. Export CSV as "all_bonus.csv"
3. Play game
4. Test if too generous
5. Compare to normal

### **Scenario 2: No Priorities**
1. Set ALL priorities to 1
2. Makes visit order random
3. Test chaos factor
4. Compare to ordered visits

### **Scenario 3: One Attraction Per Category**
1. Delete all but 1 attraction per category
2. Ultra-simple market
3. Fast games
4. Good for teaching rules

### **Scenario 4: Your Custom Set**
1. Use YOUR exact list from image
2. Copy to CSV
3. Import into editor
4. Play with exact setup

---

## ⚠️ Important Notes

### **Changes Apply to Next Game**
- Editing doesn't affect current game
- Start new game to use changes
- Good for testing without disruption

### **Backup Your Attractions**
- Export to CSV before major changes
- Keep backup files: `attractions_backup.csv`
- Can always reset to defaults

### **Game Balance**
- More attractions = longer games
- Higher pips = harder for tourists
- More bonus = more money in economy
- Gray effects = more complexity

### **CSV Editing Tips**
- Use Excel for easy editing
- Can sort/filter in Excel
- Bulk changes faster in CSV
- Import when done

---

## 🎮 Integration with Game

### **Attraction Editor Changes Affect:**
- ✅ Market initialization
- ✅ Attraction deck creation
- ✅ Shuffle and distribution
- ✅ All game statistics

### **Does NOT Change:**
- Cities on board
- Bus starting positions
- Player starting money
- Game rules

---

## 📝 Quick Reference

**Keyboard Shortcuts:**
- None currently (click-based interface)

**Important Buttons:**
- **Save Changes**: Commit edits to selected attraction
- **Delete**: Remove attraction permanently
- **Duplicate**: Copy selected attraction
- **Reset to Defaults**: Restore original 76 attractions

**File Menu:**
- **Load CSV**: Replace all attractions with CSV data
- **Save CSV**: Export all attractions
- **Reset**: Go back to defaults

---

## 🚀 Ready to Customize!

The Attraction Editor gives you **complete control** over the game economy. You can:

1. ✅ Use your EXACT attraction list from the image
2. ✅ Test different game balances
3. ✅ Create themed attraction sets
4. ✅ Share custom sets via CSV
5. ✅ Rapidly iterate on game design

**Start by:**
1. Clicking "⚙️ Edit Attractions"
2. Export to CSV to see format
3. Edit one attraction to test
4. Start new game to try it

Enjoy designing your perfect Bodensee game! 🎲🏔️
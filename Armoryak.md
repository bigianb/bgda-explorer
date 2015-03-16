#Thoughts on the Armor.yak file

The start of the file consists of structures, each 0x30 bytes long.

Each of these structures consists of 4 children, each 0x0c bytes long. Each child corresponds to a character class (Human male, female, dwarf)

These children consist of 3 integers (little ending, 4 bytes each).

The first integer is an offset to a texture from the start of the vif data.

The second integer is an offset to vif data. It's length is textureOffset.

The third integer is the length of the vif data and texture data.

so, something like this:
```
struct Child
{
int textureOffset;
int vifOffset;
int vifLength;
}

struct Entry
{
   Child[4] children;
}
```
The start of the file then consists of an array of Entry structures.
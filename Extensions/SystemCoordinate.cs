/* 
 * Starship Server
 * 
 * This file is a part of Starship Server.
 * Starship Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starship Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starship Server. If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.goodstuff.Starship.Extensions
{
    public class SystemCoordinate
    {
        public string _sector;
        public int _x;
        public int _y;
        public int _z;

        public SystemCoordinate(string sector, int x, int y, int z)
        {
            _sector = sector;
            _x = x;
            _y = y;
            _z = z;
        }

        public override string ToString()
        {
            return _sector + ":" + _x + ":" + _y + ":" + _z;
        }

        public bool Equals(WorldCoordinate test)
        {
            return this.ToString() == test.ToString();
        }
    }
}

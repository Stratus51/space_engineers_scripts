#!/usr/bin/lua
local function ln(v)
    return math.log(v)
end

local function calc_g_degen(Rh, gp, v, gmin)
    return ln(gmin/gp)/ln(gmin*Rh/v/v)
end

local function calc_orbit_radius(Rh, gp, v, n)
    return math.pow(gp*math.pow(Rh, n)/v/v, 1/(n-1))
end

local function calc_gravity(Rh, gp, n, r)
    return gp*math.pow(Rh/r, n)
end

local function parse_parameters(args, mandatory)
    mandatory = mandatory or {}

    local params = {}
    for _, a in ipairs(args) do
        local name, value = a:match("([^=]+)=(.+)")
        if name then
            params[name] = value
        end
    end

    local missing = {}
    for _, name in ipairs(mandatory) do
        if not params[name] then
            table.insert(missing, name)
        end
    end
    if #missing > 0 then
        return nil, "Missing parameters: "..table.concat(missing, ", ")
    end

    return params
end

local function main()
    local params, err = parse_parameters(arg, {"R", "max_hills", "gp", "v", "gmin"})
    if not params then
        print("Bad parameters.")
        print(err)
        os.exit(-1)
    end
    local Rh = params.R*params.max_hills
    local gp = params.gp
    local v = params.v
    local gmin = gp*params.gmin

    local res_1, res_2 = calc_g_degen(Rh, gp, v, gmin)
    if not res_1 then
        print("No solution. Delta = " + res_2)
        return
    end

    print("Solutions:")
    for i, n in ipairs{res_1, res_2} do
        print("  - ["..i.."]: ")
        print(string.format("    * gravity degenerescence power = %.2f", (n)))
        local radius = calc_orbit_radius(Rh, gp, v, n)
        print(string.format("    * orbit radius = %.2fkm", (radius/1000)))
        local gravity = calc_gravity(Rh, gp, n, radius)
        print(string.format("    * gravity on orbit = %.2f", gravity))
    end
end

main()

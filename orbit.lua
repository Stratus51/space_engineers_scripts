#!/usr/bin/lua

local function vector_norm_sq(v)
    return v.x*v.x + v.y*v.y + v.z*v.z
end

local function vector_norm(v)
    return math.sqrt(vector_norm_sq(v))
end

local function normalize_vector(v)
    local norm = vector_norm(v)
    return {
        x = v.x/norm,
        y = v.y/norm,
        z = v.z/norm,
    }
end

local function mul_vector(v, a)
    return {
        x = a*v.x,
        y = a*v.y,
        z = a*v.z,
    }
end

local function add_vector(a, b)
    return {
        x = a.x + b.x,
        y = a.y + b.y,
        z = a.z + b.z,
    }
end

local function sub_vector(a, b)
    return {
        x = a.x - b.x,
        y = a.y - b.y,
        z = a.z - b.z,
    }
end

local function project_vector(v, ref)
    return v.x*ref.x + v.y*ref.y + v.z*ref.z
end

local function print_vector(v, head, foot)
    print((head or "").."x = "..v.x.."; y = "..v.y.."; z = "..v.z..(foot or ""))
end

local ship_m = {}
function ship_m:distance()
    return vector_norm(self.pos)
end

function ship_m:speed_sq()
    return vector_norm_sq(self.speed)
end

function ship_m:orbital_speed()
    local radius_vector = normalize_vector(self.pos)
    local radial_speed = project_vector(radius_vector, self.speed)
    local tangential_speed = vector_norm(sub_vector(self.speed, mul_vector(radius_vector, radial_speed)))
    return radial_speed, tangential_speed
end

function ship_m:centrifugal_acceleration(dist)
    dist = dist or self:distance()
    return self:speed_sq() / dist
end

function ship_m:gravity_acceleration(dist)
    local opt = self.opt
    dist = dist or self:distance()
    local pow_mul = (opt.max_hill * opt.R) / dist
    local pow = 1
    for _ = 1, opt.g_power do
        pow = pow * pow_mul
    end
    return opt.g * pow
end

function ship_m:acceleration(dist)
    return -self:gravity_acceleration(dist)
end

function ship_m:move(t_step)
    local dist = self:distance()
    local acceleration_value = self:acceleration(dist)

    local accel = mul_vector(normalize_vector(self.pos), acceleration_value)

    -- print_vector(mul_vector(self.speed, t_step), "pos_inc: ")
    self.pos = add_vector(self.pos, mul_vector(self.speed, t_step))
    -- print_vector(mul_vector(accel, t_step), "speed_inc: ")
    self.speed = add_vector(self.speed, mul_vector(accel, t_step))

    return acceleration_value
end

local function init()
    local opt = {
        g = assert(tonumber(arg[1])),
        g_power = assert(tonumber(arg[2])),
        R = assert(tonumber(arg[3])),
        max_hill = assert(tonumber(arg[4])),
        v_init = assert(tonumber(arg[5])),

        t_step = assert(tonumber(arg[6])),
        print_step = assert(tonumber(arg[7])),
        min_h = assert(tonumber(arg[8])),
        max_h = assert(tonumber(arg[9])),
        max_t = assert(tonumber(arg[10])),
    }
    opt.min_h = opt.min_h * opt.R

    print("Options:")
    for k, v in pairs(opt) do
        print("  - "..k.." = "..v)
    end

    local function calc_orbit_radius(g, g_power, R, max_hill, v)
        return math.pow(g*math.pow(max_hill*R, g_power)/v/v, 1/(g_power-1))
    end
    local orbit_radius = calc_orbit_radius(opt.g, opt.g_power, opt.R, opt.max_hill, opt.v_init)

    opt.max_h = opt.max_h * orbit_radius

    local ship = {
        opt = opt,

        pos = {
            x = orbit_radius,
            y = 0,
            z = 0,
        },
        speed = {
            x = 0,
            y = opt.v_init,
            z = 0,
        },
    }
    for k, v in pairs(ship_m) do
        ship[k] = v
    end
    return ship
end
local opt_ship = init()

local function sim(ship)
    local opt = ship.opt
    local t_step = opt.t_step
    local print_step = opt.print_step
    local min_h = opt.min_h
    local max_h = opt.max_h
    local max_t = opt.max_t

    local last_print = -print_step
    for t = 0, max_t, t_step do
        local accel = ship:move(t_step)
        local dist = ship:distance()

        local function print_state()
            local radial_speed, tangential_speed = ship:orbital_speed()
            print(
                "t: "
                ..t
                .."s; dist: "
                ..dist
                .."; speed: radial = "
                ..radial_speed
                .." | tangent = "
                ..tangential_speed
                .."; accel: "
                ..accel
            )
            -- print_vector(ship.pos, "pos: ")
            -- print_vector(ship.speed, "speed: ")
        end

        if t - last_print >= print_step then
            print_state()
            last_print = t
        end

        if dist < min_h then
            print_state()
            print("Ship too low, stopping.")
            return
        elseif dist > max_h then
            print_state()
            print("Ship too high, stopping.")
            return
        end
    end
    print("Simulation done: t >= max_t ("..max_t.."s)")
end
sim(opt_ship)
print("Final")
print_vector(opt_ship.pos, "  pos: ")
print_vector(opt_ship.speed, "  speed: ")

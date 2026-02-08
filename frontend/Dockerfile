# ==========================================
# AI Question Bank - Frontend Dockerfile
# ==========================================
# 多阶段构建,使用nginx提供静态文件服务

# ==========================================
# 阶段1: 依赖安装
# ==========================================
FROM node:18-alpine AS deps

# 安装pnpm(可选,也可使用npm)
RUN npm install -g pnpm

# 设置工作目录
WORKDIR /app

# 复制package文件
COPY package.json package-lock.json pnpm-lock.yaml* ./

# 安装依赖
RUN npm ci || pnpm install --frozen-lockfile

# ==========================================
# 阶段2: 构建
# ==========================================
FROM node:18-alpine AS builder

# 设置工作目录
WORKDIR /app

# 复制依赖
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# 构建参数
ARG VITE_API_BASE_URL=http://localhost:5000

# 构建应用
RUN npm run build

# ==========================================
# 阶段3: 运行时
# ==========================================
FROM nginx:1.25-alpine AS final

# 安装curl用于健康检查
RUN apk add --no-cache curl

# 从构建阶段复制构建产物
COPY --from=builder /app/dist /usr/share/nginx/html

# 复制nginx配置
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 创建非root用户
RUN addgroup -g 1000 nginx-user && \
    adduser -D -u 1000 -G nginx-user nginx-user && \
    chown -R nginx-user:nginx-user /usr/share/nginx/html && \
    chown -R nginx-user:nginx-user /var/cache/nginx && \
    chown -R nginx-user:nginx-user /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown -R nginx-user:nginx-user /var/run/nginx.pid

# 切换到非root用户
USER nginx-user

# 暴露端口
EXPOSE 80

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
    CMD curl -f http://localhost:80/ || exit 1

# 启动nginx
CMD ["nginx", "-g", "daemon off;"]

# How to Host WhackAEllie Support Site on GitHub Pages

## Step 1: Create GitHub Repository

1. Go to [github.com](https://github.com) and log in
2. Click the "+" icon → "New repository"
3. Name it: `whackaellie-support` (or any name you prefer)
4. Make it **Public**
5. Click "Create repository"

## Step 2: Upload the HTML File

### Option A: Using GitHub Web Interface (Easiest)
1. In your new repository, click "uploading an existing file"
2. Drag and drop the `index.html` file from this project
3. Commit the file

### Option B: Using Git Command Line
```bash
cd /Users/sethchun/Documents/WhackAEllie
git init
git add index.html
git commit -m "Add support website"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/whackaellie-support.git
git push -u origin main
```
(Replace YOUR_USERNAME with your GitHub username)

## Step 3: Enable GitHub Pages

1. In your repository, go to **Settings**
2. Scroll down to **Pages** section (in the left sidebar)
3. Under "Source", select **main** branch
4. Click **Save**
5. Wait 1-2 minutes for deployment

## Step 4: Access Your Site

Your site will be available at:
```
https://YOUR_USERNAME.github.io/whackaellie-support/
```

## Step 5: Use in App Store Connect

When submitting to the App Store, use this URL as your:
- **Support URL**
- **Marketing URL** (optional)
- **Privacy Policy URL**: `https://YOUR_USERNAME.github.io/whackaellie-support/#privacy`

## Important Notes

### Update Email Addresses
Before publishing, update the email addresses in `index.html`:
- Change `support@whackaellie.com` to your real support email
- Change `privacy@whackaellie.com` to your real email

You can use:
- Your personal email
- A Gmail/iCloud email
- A custom domain email if you have one

### Custom Domain (Optional)
If you want to use a custom domain like `whackaellie.com`:
1. Buy domain from provider (GoDaddy, Namecheap, etc.)
2. In GitHub Pages settings, add custom domain
3. Update DNS settings at your domain provider
4. Full instructions: [GitHub Custom Domain Docs](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site)

## Testing

Before submitting to App Store:
1. Visit your GitHub Pages URL
2. Check all links work
3. Test on mobile browser
4. Verify email links open correctly
5. Read through privacy policy and terms

## Making Updates

To update the site after initial deployment:
1. Edit `index.html` locally
2. Commit and push changes to GitHub
3. Site will auto-update in 1-2 minutes

## Troubleshooting

**Site not loading?**
- Wait a few minutes after enabling Pages
- Check that repository is Public
- Verify `index.html` is in the root directory
- Check Settings → Pages shows "Your site is live at..."

**Wrong URL?**
- URL format is always: `https://USERNAME.github.io/REPOSITORY-NAME/`
- Make sure you're using the correct username and repo name

